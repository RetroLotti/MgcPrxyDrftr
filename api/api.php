<?php

  require("helper.php");
  $config = require "/var/config.php";

  if (!class_exists('Redis')) { die(json_encode(['error' => 'Die Redis-Klasse ist nicht verfügbar. Bitte installiere und aktiviere die Redis PHP-Erweiterung.'])); }
  if (!function_exists('msgpack_pack')) { die(json_encode(['error' => 'Die MessagePack-Erweiterung ist nicht verfügbar. Bitte installiere und aktiviere die MessagePack PHP-Erweiterung.'])); }

  // our own helper package
  $helper = new Helper();
  
  // Redis Connection
  $redis = new Redis();
  $redis->connect($config['redisserver'], 6379);

  // generate a booster pack here. This is just a stub right now
  function generateBoosterPack($set, $count = 1) 
  {

    // TODO: implement logic
    
      // Beispiel-Datenstruktur für Booster Pack
      $boosterPack = [
          'set' => $set,
          'count' => $count,
          'cards' => []
      ];

      // Hier könntest du die Kartenlogik hinzufügen
      for ($i = 0; $i < $count; $i++) {
          $boosterPack['cards'][] = "Card " . ($i + 1);
      }

      return $boosterPack;
  }

  // CORS Header
  header('Access-Control-Allow-Origin: *');
  header('Access-Control-Allow-Methods: GET, OPTIONS');
  header('Access-Control-Allow-Headers: Content-Type');

  // API Endpoint
  $method = $_SERVER['REQUEST_METHOD'];
  $path = explode('/', trim($_SERVER['REQUEST_URI'], '/'));

  if (count($path) === 2 && $path[0] === 'pack') 
  {
      $set = $path[1];

      // Parameter setzen
      $count = 1;

      if ($method === 'GET') 
      {
          // GET Parameter auslesen
          $count = isset($_GET['count']) ? intval($_GET['count']) : 1;
      } 
      else 
      {
          http_response_code(405);
          echo json_encode(['error' => 'Method Not Allowed']);
          exit;
      }

      // Booster Pack generieren
      $boosterPack = generateBoosterPack($set, $count);

      // Msgpack serialisieren
      $packedData = msgpack_pack($boosterPack);

      // TODO: save in redis
      //$redis->set('booster_pack:' . $set, $packedData);

      // compress response
      $jsonResponse = json_encode($boosterPack);
      $compressedResponse = gzencode($jsonResponse);

      // set Response-Header
      header('Content-Encoding: gzip');
      header('Content-Type: application/json');
      header('Content-Length: ' . strlen($compressedResponse));

      // send response
      echo $compressedResponse;
  } elseif (count($path) === 2 && $path[0] === 'lands') {

    $set = $path[1];
    $amount = 5; // five of each card

    $response = $helper->curlFetch("https://api.scryfall.com/cards/search?q=is:nonfoil+t:basic+s:$set+is:booster+game:paper+unique:prints");

    // pack response
    //$packedData = msgpack_pack($response);
    //$redis->set('booster_pack:' . $set, $packedData);

    // compress response
    $jsonResponse = json_encode($response);
    $compressedResponse = gzencode($jsonResponse);

    // set Response-Header
    header('Content-Encoding: gzip');
    header('Content-Type: application/json');
    header('Content-Length: ' . strlen($compressedResponse));

    // send response
    echo $compressedResponse;

    //$body = http_parse_message(http_get("https://api.scryfall.com/cards/search?q=is:nonfoil+t:basic+s:unf+is:booster+game:paper+unique:prints", array("timeout" => 1), $info))->body;
    //print_r($body);

    // TODO: 
    // i.e all 3ed duallands
    // https://api.scryfall.com/cards/search?q=is:dual+s:leb
    // all unfinity galaxy lands
    // https://api.scryfall.com/cards/search?q=is:nonfoil+t:basic+s:unf+is:booster+game:paper+unique:prints


  } else {
      http_response_code(404);
      echo json_encode(['error' => 'Not Found']);
  }
?>
