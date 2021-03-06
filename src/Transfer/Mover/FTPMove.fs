﻿

module Mover.FTPMove
open FluentFTP
open System
open Types
open System.Threading
open LoggingFsharp
let ftpClient ftpData=
    new FtpClient(ftpData.Host,ftpData.User,ftpData.Password)
let ftpResToTransRes (ftpResult:FtpStatus)=
    match ftpResult with
    |FtpStatus.Failed->TransferResult.Failed
    |FtpStatus.Success->TransferResult.Success
    |_-> failwith "ftpresult return unhandled enum value"
let makeReturn (client:FtpClient) result returnClient =
    if returnClient then
        client.Dispose()
        (result,Some client)
    else  (result,None)
let FTPtoFTP sourceFTPData destFTPData  sourceFilePath destFilePath callBack (ct:CancellationToken)=async{
    use sourceClient=ftpClient sourceFTPData
    sourceClient.Connect()
    use destClient=ftpClient destFTPData
    destClient.Connect()

    Lgverbosef "{Mover} Opening FTP writeStream for file %s" destFilePath
      
    use destStream= destClient.OpenWrite(destFilePath)

    try 
        //We download the file from the server into the local Stream.
        //TODO this needs to be tested with an out stream slower than the in one
        let! writeResult=Async.AwaitTask<|sourceClient.DownloadAsync(destStream,sourceFilePath,int64 0,callBack,ct)
        destStream.Close()
        let! reply=Async.AwaitTask <|destClient.GetReplyAsync(ct)

        //We need to check the ftp status code returned after the write completed
        let res=
            if not reply.Success then
                Lgerrorf "{Mover} Ftp writer returned failure: %A"reply
                TransferResult.Failed
            else
                match writeResult with 
                |true->TransferResult.Success
                |false->TransferResult.Failed
        let out =
            if (res = TransferResult.Cancelled
                || res = TransferResult.Failed) then
                try
                    Lgwarnf "{Mover} Cancelled or failed. Deleting ftp file %s" destFilePath
                    sourceClient.DeleteFile destFilePath
                    res
                with _ ->
                    Lgerrorf "{Mover} Cancelled or failed and was unable to delete output ftp file %s" destFilePath
                    TransferResult.Failed
            else
                res
        return out
          
    with 
    | :? OperationCanceledException-> return TransferResult.Cancelled 
}
let downloadFTP ftpData filePath destFilePath callBack (ct:CancellationToken)=async{
    use client=new FtpClient(ftpData.Host,ftpData.User,ftpData.Password)
    client.Connect()
    let task= 
        Async.AwaitTask(
            client.DownloadFileAsync (
                destFilePath,
                filePath,
                FtpLocalExists.Overwrite,
                FtpVerify.Throw,
                callBack ,
                ct ))
    try 
        let! status= task
        return ftpResToTransRes status
    with 
    | :? OperationCanceledException-> return TransferResult.Cancelled 
}
let uploadFTP ftpData filePath destFilePath callBack (ct:CancellationToken)=async{
    use client=new FtpClient(ftpData.Host,ftpData.User,ftpData.Password)
    client.Connect()
    let task= 
        Async.AwaitTask(
            client.UploadFileAsync (
                filePath,
                destFilePath,
                FtpRemoteExists.Overwrite,
                false,
                FtpVerify.Throw,
                callBack ,
                ct ))
    try 
        let! status= task
        return ftpResToTransRes status
    with 
    | :? OperationCanceledException-> return TransferResult.Cancelled 
}
