public class FoundItem
{
	public string ResourceId { get; set; }
	public int Amount { get; set; }
	
	public FoundItem(string resourceId, int amount = 1)
	{
		ResourceId = resourceId;
		Amount = amount;
	}
}
