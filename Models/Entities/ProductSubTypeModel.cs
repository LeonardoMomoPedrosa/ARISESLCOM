namespace ARISESLCOM.Models.Entities
{
    public class ProductSubTypeModel
    {
        public int PKId { get; set; }
        public int TypeId { get; set; }
        public bool Visible { get; set; }
        public string SubType { get; set; }
        public string Description { get; set; }
        public string MetaName { get; set; }
        public string MetaKeys { get; set; }
    }
}
