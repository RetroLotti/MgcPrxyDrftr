<?php
	header('Content-Type: application/json');

	require("helper.php");
    $config = require "/var/config.php";

	if (!class_exists('Redis')) {
	    die(json_encode(['error' => 'Redis-Class is not available. Please install and activate the Redis PHP-Extension.']));
	}
	if (!function_exists('msgpack_pack')) {
    	die(json_encode(['error' => 'MessagePack-Extension is not available. Please install and activate the MessagePack PHP-Extension.']));
	}

	$helper = new Helper();

  	$redis = new Redis();
	$redis->connect($config['redisserver'], 6379);

	try {

    	// sets => sorted by sets with available products
    	// products => sorted by products with available sets
    
    	$data = [
        	"sets" => [],
        	"products" => []
    	];
       
		$dsn = "mysql:host={$config['databaseserver']};dbname={$config['database']};charset=utf8mb4";
		$options = [
		    PDO::ATTR_ERRMODE            => PDO::ERRMODE_EXCEPTION,
		    PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
	    	PDO::ATTR_EMULATE_PREPARES   => false,
		];
	    
		try {
    		$pdo = new PDO($dsn, $config['databaseusername'], $config['databasepassword'], $options);
		} catch (\PDOException $e) {
    		error_log($e->getMessage());
    		if (ini_get('display_errors')) {
        		echo 'Datenbankverbindungsfehler. Bitte versuchen Sie es sp채ter erneut.';
	    	} else {
    	    	echo 'Ein Fehler ist aufgetreten. Bitte kontaktieren Sie den Administrator.';
	    	}
	    	exit;
		}
    
    	$cacheKey = "boosteroptions";
    	if ($redis->exists($cacheKey)) {
        	$data = $helper->unpackFromRedis($redis, $cacheKey);
        } else {
        	$stmt = $pdo->prepare("select distinct s.code, sbc.boosterName from sets s, setBoosterContents sbc where sbc.setCode = s.code order by 1, 2");
    		$stmt->execute();
    		$sets = $stmt->fetchAll(PDO::FETCH_ASSOC);
    
    		$stmt = $pdo->prepare("select distinct sbc.boosterName, s.code from sets s, setBoosterContents sbc where sbc.setCode = s.code order by 1, 2");
    		$stmt->execute();
    		$products = $stmt->fetchAll(PDO::FETCH_ASSOC);
    	
    		foreach($sets as $setValue) {
    	    	$data["sets"][$setValue["code"]][] = $setValue["boosterName"];
	        }
    
    		foreach($products as $productValue) {
    	    	$data["products"][$productValue["boosterName"]][] = $productValue["code"];
	        }
        
        	$helper->packToRedis($redis, $cacheKey, $data, $config["timeToLive"]);
        }
    
		if (substr_count($_SERVER['HTTP_ACCEPT_ENCODING'], 'gzip')) {
        	ob_start("ob_gzhandler");
    	} else {
	        ob_start();
    	}

    	echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    	ob_end_flush();
    
    } catch (Exception $e) {
    	http_response_code(500);
    	echo json_encode(['error' => $e->getMessage()]);
    }
?>