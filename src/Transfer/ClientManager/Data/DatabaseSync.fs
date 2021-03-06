namespace ClientManager.Data
open System.Collections.Generic
open System.Collections.Specialized
open System;
open System.Threading
open System.IO
open SharedFs.SharedTypes;
open ClientManager.Data.DataBase
 module DataBaseSync=
    let mergeChanges userName changes=
        lock dataBase (fun ()->
        for transData in changes.TransferDataList  do
            dataBase.[userName].TransferDataList.[transData.Key]<-transData.Value
        if changes.Jobs.Length> 0 then
            dataBase.[userName].Jobs<-changes.Jobs
        )
(**
    let internal syncIndexLevel userName groupName (changes:Dictionary<int,TransferData>)=
        if not(dataBase.ContainsKey groupName) then 
           dataBase.Add(groupName,new Dictionary<string,Dictionary<int, TransferData >>()  ) 
          //clientId exists?
        if not(dataBase.[groupName].ContainsKey userName) then
            dataBase.[groupName].Add(userName,new Dictionary<int, TransferData >()  )

        let indexs= seq changes.Keys
        let transferDatas= seq changes.Values
        (indexs,transferDatas)||>Seq.iter2(fun index transData->
            setTransferData transData groupName userName index )

    let internal syncGrouplevel userName (changes:Dictionary<string, Dictionary<int,TransferData>>)=
         
      
        let groupNames= seq changes.Keys
        let transferDatas= seq changes.Values
        (groupNames,transferDatas)||>Seq.iter2(fun groupName changedData->syncIndexLevel userName groupName changedData )
   
    let syncDataBaseChanges userName (changes:Dictionary<string, Dictionary<int,TransferData>>)=
        syncGrouplevel userName changes *)