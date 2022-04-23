namespace Services.HashGeneratorService
{
	public interface IHashGenerator
	{
		string GetHash(object value);
	}
}