using System.Collections.Generic;

namespace EasyRun.Tye.DTOS
{
    public class TyeServiceDTO
    {
        public TyeServiceType ServiceType { get; set; }
        public Dictionary<string, TyeReplicaStatusDTO> Replicas { get; set; }
    }
}
