using Service.StoreMessages.Enums;

namespace Service.NCFS
{
    public class NcfsOutcome
    {
        public NcfsDecision NcfsDecision { get; set; }
        public string Base64Replacement { get; set; }
    }
}
