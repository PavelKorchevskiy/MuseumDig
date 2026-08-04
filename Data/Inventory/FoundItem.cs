public class FoundItem
{
	public string ResourceId { get; set; }
	public Quality Quality { get; set; }
	public int Amount { get; set; }
	
	public FoundItem(string resourceId, Quality quality = Quality.Good, int amount = 1)
	{
		ResourceId = resourceId;
		Quality = quality;
		Amount = amount;
	}
}
