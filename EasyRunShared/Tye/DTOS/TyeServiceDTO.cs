using System.Collections.Generic;

namespace EasyRun.Tye.DTOS
{
    public class TyeServiceDTO
    {
        public TyeDescriptionDTO Description { get; set; }
        public TyeServiceType ServiceType { get; set; }
        public Dictionary<string, TyeReplicaStatusDTO> Replicas { get; set; }
    }
}
