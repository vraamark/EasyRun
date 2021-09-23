namespace EasyRun.Tye.DTOS
{
    public class TyeReplicaStatusDTO
    {
        public string Name { get; set; }
        public int Pid { get; set; }        
        public TyeReplicaState State { get; set; }
    }
}
