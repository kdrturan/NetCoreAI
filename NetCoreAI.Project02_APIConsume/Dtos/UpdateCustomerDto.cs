namespace NetCoreAI.Project02_APIConsume.Dtos
{
    public class UpdateCustomerDto
    {
        public int customerId { get; set; }
        public string customerName { get; set; }
        public string customerLastName { get; set; }
        public decimal customerBalance { get; set; }
    }
}
