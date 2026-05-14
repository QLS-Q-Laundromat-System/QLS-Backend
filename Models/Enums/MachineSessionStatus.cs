namespace QLS.Backend.Models.Enums
{
    public enum MachineSessionStatus
    { 
        PendingPayment = 0,     
        Running = 1,        
        Completed = 2,
        Cancelled = 3,
        Error = 4,
        PaidWaitingForStart = 5
    }
}
