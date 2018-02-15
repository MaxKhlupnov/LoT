<?php
$action= @$_REQUEST['action'];
$stoken= @$_REQUEST['stoken'];
$appctx= @$_REQUEST['appctx'];

$newtoken=urlencode($stoken);

$url = parse_url($appctx);
$breakthepath = explode("/",$url["path"]);
$homeid=$breakthepath[1];


if($action=="login")
{
        $redirecturl = $url["scheme"]."://".$url["host"].":".$url["port"]."/".$homeid."/auth/"."redirect?stoken=".$newtoken."&appctx=".$appctx."&action=".$action."&scheme=liveid";
//      echo $redirecturl;
        header("Location: ".$redirecturl);
}

if($action=="logout")
{
        echo "<h1>Logged out.</h1> <script type='text/javascript'> window.close(); </script> ";
}



?>
