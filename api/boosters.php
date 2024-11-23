<?php
    header('Content-Type: application/json');

    require "helper.php";
    $config = require "/var/config.php";

	if (!class_exists('Redis')) {
	    die(json_encode(['error' => 'Redis-Class is not available. Please install and activate the Redis PHP-Extension.']));
	}
	if (!function_exists('msgpack_pack')) {
    	die(json_encode(['error' => 'MessagePack-Extension is not available. Please install and activate the MessagePack PHP-Extension.']));
	}

    try {

        $redis = new Redis();
        $redis->connect($config['redisserver'], 6379);

        $helper = new Helper();

        $dsn = "mysql:host={$config['databaseserver']};dbname={$config['database']};charset=utf8mb4";
        $options = [
            PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
            PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            PDO::ATTR_EMULATE_PREPARES   => false,
        ];

        $pdo = new PDO($dsn, $config['databaseusername'], $config['databasepassword'], $options);

        $setCode = isset($_GET['s']) ? strtoupper($_GET['s']) : 'LEB';
        $boosterName = isset($_GET['b']) ? strtolower($_GET['b']) : 'default';
        $amount = isset($_GET['a']) ? intval($_GET['a']) : 1;

        $data = [
            "set" => $setCode,
            "product" => $boosterName,
            "booster" => []
        ];
       
    	$cacheKey = "boosteroptions";
    	$boosterOptions = $redis->exists($cacheKey) ? $helper->unpackFromRedis($redis, $cacheKey) : fetachAndCacheBoosterOptions($pdo, $config, $helper, $redis, $cacheKey);
    
    	// TODO: check given api token (X-API-KEY) file or database?!?
    	//
		
    	// check if given set is valid
    	if ( !isset($boosterOptions['sets'][$setCode])) { 
        	http_response_code(404);
        	echo json_encode(['error' => 'Set not found.']);
        	exit;
        }
    
    	// check if given product is available for this set
    	if ( !in_array($boosterName, $boosterOptions['sets'][$setCode])) { 
        	http_response_code(404);
        	echo json_encode(['error' => 'Product not found for specified set.']);
        	exit;
        }	
    
    	// check if amount is too damn high
    	if ($amount > 666) { 
        	http_response_code(400);
        	echo json_encode(['error' => 'Too many boosters requested at the same time.']);
        	exit;
        }

        $cacheKey = "sumBoosterWeight:$setCode:$boosterName";
        $totalBoosterWeight = $redis->exists($cacheKey) ? $helper->unpackFromRedis($redis, $cacheKey) : fetchAndCacheTotalBoosterWeight($pdo, $config, $helper, $redis, $cacheKey, $setCode, $boosterName);

        $cacheKey = "boosterWeights:$setCode:$boosterName";
        $boosterContentList = $redis->exists($cacheKey) ? $helper->unpackFromRedis($redis, $cacheKey) : fetchAndCacheBoosterWeights($pdo, $config, $helper, $redis, $cacheKey, $setCode, $boosterName, $totalBoosterWeight);

        for ($j = 0; $j < $amount; $j++) {
            $randomWeightedBoosterIndex = $helper->getRandomWeightedElement($boosterContentList);

            $cacheKey = "sheets:$setCode:$boosterName:$randomWeightedBoosterIndex";
            $sheetList = $redis->exists($cacheKey) ? $helper->unpackFromRedis($redis, $cacheKey) : fetchAndCacheSheets($pdo, $config, $helper, $redis, $cacheKey, $setCode, $boosterName, $randomWeightedBoosterIndex);

            $cards = [];
            $foilCards = [];
            $overallCounter = -1;

            foreach ($sheetList as $value) {
                $sheetName = $value['sheetName'];
                $isFixed = $value['sheetIsFixed'];
                $isFoil = $value['sheetIsFoil'];

                $cacheKey = "sumSheetWeight:$setCode:$boosterName:$sheetName";
                $totalSheetWeight = $redis->exists($cacheKey) ? $helper->unpackFromRedis($redis, $cacheKey) : fetchAndCacheTotalSheetWeight($pdo, $config, $helper, $redis, $cacheKey, $setCode, $boosterName, $sheetName);

                $cacheKey = "sumSheetCardsWeight:$setCode:$boosterName:$sheetName";
                $cardList = $redis->exists($cacheKey) ? $helper->unpackFromRedis($redis, $cacheKey) : fetchAndCacheSheetCardsWeight($pdo, $config, $helper, $redis, $cacheKey, $setCode, $boosterName, $sheetName, $totalSheetWeight, $isFixed);

                if ($isFixed == 1) {
                    foreach ($cardList as $weightCardKey => $weightCardValue) {
                        for ($i = 1; $i <= $weightCardValue; $i++) {
                            if ($isFoil == 1) {
                                $foilCards[++$overallCounter . "_" . $sheetName] = $weightCardKey;
                            } else {
                                $cards[++$overallCounter . "_" . $sheetName] = $weightCardKey;
                            }
                        }
                    }
                } else {
                    for ($k = 1; $k <= $value['sheetPicks']; $k++) {
                        do {
                            $weightedSheetPick = $helper->getRandomWeightedElement($cardList);
                        } while (array_key_exists($weightedSheetPick, $cards));

                        if ($isFoil == 1) {
                            $foilCards[++$overallCounter . "_" . $sheetName] = $weightedSheetPick;
                        } else {
                            $cards[++$overallCounter . "_" . $sheetName] = $weightedSheetPick;
                        }
                    }
                }
            }

            $newBooster = ["cards" => []];
            foreach ($cards as $uuid) {
                $cacheKey = "cards:$setCode:$uuid";
                $cardInfo = $redis->exists($cacheKey) ? $helper->unpackFromRedis($redis, $cacheKey) : fetchAndCacheCardDetails($pdo, $config, $helper, $redis, $cacheKey, $setCode, $uuid);
                addCardToBooster($newBooster["cards"], $cardInfo[0], $setCode, $helper, $config, $redis, $pdo);
            }
            foreach ($foilCards as $uuid) {
                $cacheKey = "cards:$setCode:$uuid";
                $cardInfo = $redis->exists($cacheKey) ? $helper->unpackFromRedis($redis, $cacheKey) : fetchAndCacheCardDetails($pdo, $config, $helper, $redis, $cacheKey, $setCode, $uuid);
                addCardToBooster($newBooster["cards"], $cardInfo[0], $setCode, $helper, $config, $redis, $pdo, true);
            }

            $data["booster"][] = $newBooster;
        }

        if (substr_count($_SERVER['HTTP_ACCEPT_ENCODING'], 'gzip')) {
            ob_start("ob_gzhandler");
        } else {
            ob_start();
        }

        echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
        ob_end_flush();

    } catch (PDOException $e) {
        http_response_code(500);
        echo json_encode(['error' => $e->getMessage()]);
    }

    function addCardToBooster(&$cardList, $cardInfo, $setCode, $helper, $config, &$redis, &$pdo, $isFoil = false) {
        $newCard = [
            "uuid" => $cardInfo["uuid"],
            "scryfallid" => $cardInfo["scryfallid"],
            "name" => $cardInfo["name"],
            "text" => $cardInfo["text"],
            "flavorText" => $cardInfo["flavorText"],
            "type" => $cardInfo["type"],
            "manaCost" => $cardInfo["manaCost"],
            "side" => $cardInfo["side"],
            "rarity" => $cardInfo["rarity"],
            "layout" => $cardInfo["layout"],
            "foil" => $isFoil,
            "otherCards" => []
        ];

        if ($cardInfo["otherfaceids"]) {
            $otherFaceIds = explode(",", $cardInfo["otherfaceids"]);
            foreach ($otherFaceIds as $additionalCardUuid) {
                $cacheKey = "cards:$setCode:$additionalCardUuid";
                $addCardInfo = $redis->exists($cacheKey) ? $helper->unpackFromRedis($redis, $cacheKey) : fetchAndCacheCardDetails($pdo, $config, $helper, $redis, $cacheKey, $setCode, $additionalCardUuid);
                $addCardInfo[0]["otherfaceids"] = null;
                addCardToBooster($newCard["otherCards"], $addCardInfo[0], $setCode, $helper, $config, $redis, $pdo, $isFoil);
            }
        }

        $cardList[] = $newCard;
    }

	function fetachAndCacheBoosterOptions($pdo, $config, $helper, $redis, $cacheKey) {
    	$stmt = $pdo->prepare("select distinct s.code, sbc.boosterName from sets s, setBoosterContents sbc where sbc.setCode = s.code order by 1, 2");
    	$stmt->execute();
    	$sets = $stmt->fetchAll(PDO::FETCH_ASSOC);
    
    	$stmt = $pdo->prepare("select distinct sbc.boosterName, s.code from sets s, setBoosterContents sbc where sbc.setCode = s.code order by 1, 2");
    	$stmt->execute();
    	$products = $stmt->fetchAll(PDO::FETCH_ASSOC);
    	
    	foreach($sets as $setValue) {
    		$boosterOptions["sets"][$setValue["code"]][] = $setValue["boosterName"];
	    }
    
    	foreach($products as $productValue) {
    	  	$boosterOptions["products"][$productValue["boosterName"]][] = $productValue["code"];
	    }
        
        $helper->packToRedis($redis, $cacheKey, $boosterOptions, $config['redistimetolive']);
    
        return $boosterOptions;
    }

    function fetchAndCacheTotalBoosterWeight($pdo, $config, $helper, $redis, $cacheKey, $setCode, $boosterName) {
        $stmt = $pdo->prepare("SELECT SUM(boosterWeight) FROM setBoosterContentWeights WHERE setCode = ? AND boosterName = ? LIMIT 1");
        $stmt->execute([$setCode, $boosterName]);
        $totalBoosterWeight = $stmt->fetchColumn();
        $helper->packToRedis($redis, $cacheKey, $totalBoosterWeight, $config['redistimetolive']);
        return $totalBoosterWeight;
    }

    function fetchAndCacheBoosterWeights($pdo, $config, $helper, $redis, $cacheKey, $setCode, $boosterName, $totalBoosterWeight) {
        $stmt = $pdo->prepare("SELECT boosterIndex, boosterWeight FROM setBoosterContentWeights WHERE setCode = ? AND boosterName = ? ORDER BY boosterIndex ASC");
        $stmt->execute([$setCode, $boosterName]);
        $boosterWeightRows = $stmt->fetchAll(PDO::FETCH_ASSOC);

        $boosterContentList = [];
        foreach ($boosterWeightRows as $contentWeightValue) {
            $boosterContentList[$contentWeightValue["boosterIndex"]] = $contentWeightValue["boosterWeight"] / $totalBoosterWeight;
        }
        $helper->packToRedis($redis, $cacheKey, $boosterContentList, $config['redistimetolive']);
        return $boosterContentList;
    }

    function fetchAndCacheSheets($pdo, $config, $helper, $redis, $cacheKey, $setCode, $boosterName, $randomWeightedBoosterIndex) {
        $stmt = $pdo->prepare("SELECT sbc.*, sbs.sheetHasBalanceColors, sbs.sheetIsFoil, sbs.sheetIsFixed FROM setBoosterContents sbc, setBoosterSheets sbs WHERE sbc.boosterIndex = ? AND sbc.setCode = ? AND sbc.boosterName = ? AND sbs.setCode = sbc.setCode AND sbs.boosterName = sbc.boosterName AND sbs.sheetName = sbc.sheetName");
        $stmt->execute([$randomWeightedBoosterIndex, $setCode, $boosterName]);
        $sheetList = $stmt->fetchAll(PDO::FETCH_ASSOC);
        $helper->packToRedis($redis, $cacheKey, $sheetList, $config['redistimetolive']);
        return $sheetList;
    }

    function fetchAndCacheTotalSheetWeight($pdo, $config, $helper, $redis, $cacheKey, $setCode, $boosterName, $sheetName) {
        $stmt = $pdo->prepare("SELECT SUM(cardWeight) AS totalSheetWeight FROM setBoosterSheetCards WHERE sheetName = ? AND boosterName = ? AND setCode = ? LIMIT 1");
        $stmt->execute([$sheetName, $boosterName, $setCode]);
        $totalSheetWeight = $stmt->fetchColumn();
        $helper->packToRedis($redis, $cacheKey, $totalSheetWeight, $config['redistimetolive']);
        return $totalSheetWeight;
    }

    function fetchAndCacheSheetCardsWeight($pdo, $config, $helper, $redis, $cacheKey, $setCode, $boosterName, $sheetName, $totalSheetWeight, $omitWeightCalculation) {
        $stmt = $pdo->prepare("SELECT carduuid, cardweight FROM setBoosterSheetCards WHERE sheetName = ? AND boosterName = ? AND setCode = ?");
        $stmt->execute([$sheetName, $boosterName, $setCode]);
        $sheetCardWeightRows = $stmt->fetchAll(PDO::FETCH_ASSOC);

        $cardList = [];
        foreach ($sheetCardWeightRows as $cardValue) {
            $cardList[$cardValue['carduuid']] = $omitWeightCalculation == 1 ? $cardValue['cardweight'] : $cardValue['cardweight'] / $totalSheetWeight;
        }
        $helper->packToRedis($redis, $cacheKey, $cardList, $config['redistimetolive']);
        return $cardList;
    }

    function fetchAndCacheCardDetails($pdo, $config, $helper, $redis, $cacheKey, $setCode, $uuid) {
        $stmt = $pdo->prepare("SELECT c.otherfaceids, c.uuid, ci.scryfallid, IF(c.faceName is null, c.name, c.faceName) as name, c.text, c.flavorText, c.type, c.manaCost, c.side, c.rarity, c.layout, CONCAT('https://cards.scryfall.io/png/front/', '', SUBSTRING(ci.scryfallId, 1, 1), '/', SUBSTRING(ci.scryfallId, 2, 1), '/', ci.scryfallId, '.png') AS frontimage, IF(ci.scryfallCardBackId = '0aeebaf5-8c7d-4636-9e82-8c27447861f7', NULL, CONCAT('https://cards.scryfall.io/png/back/', '', SUBSTRING(ci.scryfallId, 1, 1), '/', SUBSTRING(ci.scryfallId, 2, 1), '/', ci.scryfallId, '.png')) AS backimage FROM cards c JOIN cardIdentifiers ci ON ci.uuid = c.uuid WHERE c.uuid = ? LIMIT 1 OFFSET 0");
        $stmt->execute([$uuid]);
        $cardDetails = $stmt->fetchAll(PDO::FETCH_ASSOC);

        if (!empty($cardDetails["otherfaceids"])) {
            $otherCardIds = explode(',', $cardDetails["otherfaceids"]);
            $cardDetails["faces"] = $otherCardIds;
        }

        $helper->packToRedis($redis, $cacheKey, $cardDetails, $config['redistimetolive']);
        return $cardDetails;
    }
?>
