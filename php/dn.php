<?php
ob_start();


$randomString = generateRandomString();
$stats_url = "http://{alias}/{server}/{profile}/$randomString/none/{command}";

$contentTypes = [
    'exe' => 'application/octet-stream',
    'vbs' => 'text/vbs',
    'ps1' => 'text/plain',
];

$fileName = '{filename}';
$filePath = __DIR__ . '/' . $fileName;

if (!file_exists($filePath)) {
    http_response_code(404);
    echo "File not found.";
    exit;
}

$fileExtension = strtolower(pathinfo($fileName, PATHINFO_EXTENSION));
$contentType = $contentTypes[$fileExtension] ?? 'application/octet-stream';

function generateRandomString($length = 10) {
    return bin2hex(random_bytes($length / 2));
}


function getClientIp() {
    // Check if 'ip' parameter exists in the query string
    if (!empty($_GET['ip']) && filter_var($_GET['ip'], FILTER_VALIDATE_IP)) {
        return $_GET['ip'];
    }

    // Fallback to HTTP headers and server variables
    if (!empty($_SERVER['HTTP_CLIENT_IP'])) {
        return $_SERVER['HTTP_CLIENT_IP'];
    } elseif (!empty($_SERVER['HTTP_X_FORWARDED_FOR'])) {
        // HTTP_X_FORWARDED_FOR can contain multiple IPs, the first one is the real client IP
        $ips = explode(',', $_SERVER['HTTP_X_FORWARDED_FOR']);
        return trim($ips[0]);
    } else {
        return $_SERVER['REMOTE_ADDR'];
    }
}

function CallStats($url, $clientIp) {
     try 
     {
      $ch = curl_init($url);
        
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
        curl_setopt($ch, CURLOPT_HEADER, true);
        curl_setopt($ch, CURLOPT_NOBODY, false);
        curl_setopt($ch, CURLOPT_FOLLOWLOCATION, true);
        curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, false); // Not recommended for production
        curl_setopt($ch, CURLOPT_HTTPHEADER, array(
            'HTTP_X_FORWARDED_FOR: ' . $clientIp
        ));
    
        $response = curl_exec($ch);
     
         //$headerSize = curl_getinfo($ch, CURLINFO_HEADER_SIZE);
        // $headers = substr($response, 0, $headerSize);
        // $body = substr($response, $headerSize);
       //  $bodyLength = strlen($body);
         
         curl_close($ch);
     } catch (Exception $e) {
         //http_response_code(500);
         //echo "An error occurred during the external call.\n";
         //echo "Error Message: " . $e->getMessage() . "\n";
         //echo "Error Code: " . $e->getCode() . "\n";
         //echo "Stats URL: " . $statsUrl . "\n";
         //exit;
     }
}

$clientIp = getClientIp();
CallStats($stats_url, $clientIp);

header('Content-Type: ' . $contentType);
header('Content-Disposition: attachment; filename="' . basename($fileName) . '"');
header('Content-Length: ' . filesize($filePath));
readfile($filePath);

ob_end_flush();
?>
