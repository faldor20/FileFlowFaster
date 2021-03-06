﻿namespace JobManager
open System.Collections.Generic
open SharedFs.SharedTypes
open System.Linq
open System
open LoggingFsharp

///contains one schedule id for each job in each jobsource.
/// source a=[job1,job2] b=[job1] jobOrder=[(a);(b);(a)]
type JobOrder= (SourceID)ResizeArray

module JobOrder =
    ///Counts the number of SourceID's of the same type in the jobOrder before the one given
    let countBefore (jobOrder:JobOrder) countItem index=
            let mutable amount=0
            for i in [0..index-1] do
                if jobOrder.[i]=countItem then amount<-amount+1
            amount
    ///Returns each SourceID along with its count in the list
    /// eg [a,0;b,0;b,1;c,0]
    let countUp (jobOrder:IEnumerable<'a>)=
        let counts=Dictionary<'a,int>()
        jobOrder.Select(fun job->
            if not (counts.ContainsKey(job)) then counts.[job]<-0
            else counts.[job]<-counts.[job]+1
            (job,counts.[job])
        ).ToArray()

    ///Returns a list of jobs from the joborder that have all their required tokens and are avilable
    ///Removes the jobs from the JobOrder and Source 
    let takeJobsReadyToRun (jobOrder:JobOrder) (sources:SourceList)=
        let indexed=countUp jobOrder
        let jobsToRun=
            seq{
            for (jobSource,i) in indexed do
                let job=sources.[jobSource].Jobs.[i]
                if job.TakenTokens = sources.[jobSource].RequiredTokens && job.Available then
                    yield (jobSource,job.ID)
            }|>Seq.toList
        
        //Removes each job from the joborder and removes its associated source instance from the jobOrder
        jobsToRun|>List.choose(fun (source,id)->
            //this removes the job from the source list
          
            let i=sources.[source].Jobs.FindIndex (Predicate( fun x->x.ID=id))
            match  i with
            |(-1)->
                Lgerror2 "Tried to remove an instanace of source: {@srcID} from the joborder for job {@job} but it wasn't present"  source id
                None
            |a->
                sources.[source].Jobs.RemoveAt a
                Lgdebug3 "Removed job {id} from source {src} at position {pos} "id source i
            
                match jobOrder.Remove(source) with
                |true->Some(source,id)
                |false->
                    Lgerrorf "Tried to remove a job that should have been there but wasn't"
                    None
            )
              

        
 
