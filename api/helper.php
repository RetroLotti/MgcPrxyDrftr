<?php

	class Helper
	{
        public function curlFetch($url) 
        {
            $curl = curl_init();
            curl_setopt($curl, CURLOPT_URL, $url);
            curl_setopt($curl, CURLOPT_RETURNTRANSFER, 1);
            $result = curl_exec($curl);
            curl_close($curl);
      
            return json_decode($result, true);
        }

        public function packToRedis($redis, $cacheKey, $data, $timeToLive) {
            $packedData = msgpack_pack($data);
            $redis->set($cacheKey, $packedData);
            $redis->expire($cacheKey, $timeToLive);
        }

        public function unpackFromRedis($redis, $cacheKey) {
            $packedData = $redis->get($cacheKey);
            return msgpack_unpack($packedData);
        }

		public function getGUID()
		{
			if (function_exists('com_create_guid'))
			{
				return com_create_guid();
			}
			else
			{
				mt_srand((double)microtime()*10000);//optional for php 4.2.0 and up.
				$charid = strtoupper(md5(uniqid(rand(), true)));
				$hyphen = chr(45);// "-"
				$uuid = substr($charid, 0, 8).$hyphen
					.substr($charid, 8, 4).$hyphen
					.substr($charid,12, 4).$hyphen
					.substr($charid,16, 4).$hyphen
					.substr($charid,20,12);

				return $uuid;
			}
		}
		
		/**
		 * getRandomWeightedElement()
		 * Utility function for getting random values with weighting.
		 * Pass in an associative array, such as array('A'=>5, 'B'=>45, 'C'=>50)
		 * An array like this means that "A" has a 5% chance of being selected, "B" 45%, and "C" 50%.
		 * The return value is the array key, A, B, or C in this case.  Note that the values assigned
		 * do not have to be percentages.  The values are simply relative to each other.  If one value
		 * weight was 2, and the other weight of 1, the value with the weight of 2 has about a 66%
		 * chance of being selected.  Also note that weights should be integers.
		 * 
		 * @param array $weightedValues
		 */
        public function getRandomWeightedElement(array $weightedValues) {
            $totalWeight = array_sum($weightedValues);
            $itemWeightIndex = $this->getRandomFloat() * $totalWeight;
            $currentWeightIndex = 0;

            foreach ($weightedValues as $key => $value) {
                $currentWeightIndex += $value;
                if ($currentWeightIndex >= $itemWeightIndex) {
                    return $key;
                }
            }
        }

        public function getRandomFloat($st_num = 0, $end_num = 1, $mul = 1000000) {
            if ($st_num > $end_num) return false;
            return mt_rand($st_num * $mul, $end_num * $mul) / $mul;
        }

        public function fetch($result)
        {    
            $array = array();
      
        if($result instanceof mysqli_stmt)
        {
            $result->store_result();
        
            $variables = array();
            $data = array();
            $meta = $result->result_metadata();
        
            while($field = $meta->fetch_field())
                $variables[] = &$data[$field->name]; // pass by reference
        
            call_user_func_array(array($result, 'bind_result'), $variables);
        
            $i=0;
            while($result->fetch())
            {
                $array[$i] = array();
                foreach($data as $k=>$v)
                $array[$i][$k] = $v;
                $i++;
          
                // don't know why, but when I tried $array[] = $data, I got the same one result in all rows
            }
          }
          elseif($result instanceof mysqli_result)
          {
            while($row = $result->fetch_assoc())
              $array[] = $row;
          }
      
          return $array;
        }
  }
?>