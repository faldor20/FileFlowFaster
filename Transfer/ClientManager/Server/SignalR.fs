namespace ClientManager.Server
open System.Collections.Generic
open System
open Giraffe
open System.Threading;
open System.Threading.Tasks;
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Hosting
open TransferClient.JobManager;
open ClientManager.Data;
open FSharp.Json;
open SharedFs.SharedTypes
open Microsoft.AspNetCore.Mvc
module SignalR=
   (*  type IClientApi = 
      abstract member DataResponse : Dictionary<Guid,TransferData> -> Threading.Tasks.Task *)

    

    type ITransferClientApi = 
      abstract member CancelTransfer : int -> Task
      abstract member Testing :string -> Task
      abstract member ResetDB :unit -> Task

    and ClientManagerHub(manager:FrontEndManager)=
        inherit Hub<ITransferClientApi>()
        let frontEndManager=manager
        member this.GetTransferData groupName id=
           (groupName,id)||> DataBase.getTransferData
        
        member this.RegisterSelf (userName:string) =
            printfn "Registering new client. Username: %s connectionID=%s userID=%s" userName this.Context.ConnectionId  this.Context.UserIdentifier 
            DataBase.registerClient userName this.Context.ConnectionId 
            
        member this.OverwriteTransferData(userName:string) ( changes:UIData) =
            printfn "overwriting local info with client info. Username: %s connectionID=%s userID=%s" userName this.Context.ConnectionId  this.Context.UserIdentifier 
            lock(DataBase.dataBase) (fun x->
            DataBase.dataBase.[userName]<- changes
            )

        member this.SyncTransferData (userName:string) ( changes:UIData) =
            printfn "syncing transferData "
            DataBaseSync.mergeChanges userName changes
            frontEndManager.ReceiveDataChange userName changes
            printfn "Synced transferData from %s" userName  
    
    and IFrontendApi = 
      abstract member ReceiveData :Dictionary<string, UIData> -> Task
      abstract member ReceiveDataChange :string->UIData -> Task
      abstract member Testing :string -> Task
    //this apprently needs to be injected
    and ClientManager (hubContext :IHubContext<ClientManagerHub,ITransferClientApi>) =
        inherit Controller ()
        member this.HubContext :IHubContext<ClientManagerHub, ITransferClientApi> = hubContext
        member this.CancelTransfer   user id=
            let clientID = DataBase.getClientID user
            printfn "Sending Cancellation request to user:%s with connecionid %s" user clientID
            (this.HubContext.Clients.All.CancelTransfer id).Wait()
        member this.ResetDB ()=
            this.HubContext.Clients.All.ResetDB();
   

    and DataHub(manager:ClientManager)=
        inherit Hub<IFrontendApi>()
        let toDictionary (map : Map<_, _>) : Dictionary<_, _> = Dictionary(map)
        let clientManager=manager

        member this.GetTransferData()=
            let data =DataBase.dataBase
            this.Clients.All.ReceiveData(data)
        member this.GetConfirmation()=
            this.Clients.All.Testing("hiya from the other side")
        member this.CancelTransfer  user id=
            printfn "recieved Cancellation request for item %i and user %s" id user ;

            clientManager.CancelTransfer  user id
    and FrontEndManager (hubContext :IHubContext<DataHub,IFrontendApi>) =
        inherit Controller ()
        member this.HubContext :IHubContext<DataHub, IFrontendApi> = hubContext
        member this.ReceiveDataChange user change=
            (this.HubContext.Clients.All.ReceiveDataChange user change).Wait()
        member this.ReceiveData change=
            (this.HubContext.Clients.All.ReceiveData DataBase.dataBase).Wait()
    
    
             





  (*    type TransferProgressService (hubContext :IHubContext<DataHub, IClientApi>) =
        inherit BackgroundService ()
      
        member this.HubContext :IHubContext<DataHub, IClientApi> = hubContext

        override this.ExecuteAsync (stoppingToken :CancellationToken) =
            let pingTimer = new System.Timers.Timer(100)
            pingTimer.Elapsed.Add(fun _ -> 
              
              let stateSerialized = serializeGameState gState
              this.HubContext.Clients.All.GameState(stateSerialized) |> ignore)

            pingTimer.Start()
            Task.CompletedTask  *)