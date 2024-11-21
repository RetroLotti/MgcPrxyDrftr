<?php
	header('Content-Type: application/json');

	$config = require "/var/config.php";
	
	$redis = new Redis();
	$redis->connect($config['redisserver'], 6379);

	// select * from cards where setCode = 'MH3' order by length(number), number
?>