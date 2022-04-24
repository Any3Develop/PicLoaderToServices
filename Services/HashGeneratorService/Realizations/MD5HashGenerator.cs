using System;
using System.Linq;
using System.Text;

namespace Services.HashGeneratorService
{
	public class MD5HashGenerator : IHashGenerator
	{
		public string GetHash(object value)
		{
			if (value == null)
			{
				throw new ArgumentException("Input value does not exist");
			}

			using var md5 = System.Security.Cryptography.MD5.Create();
			// Use input string to calculate MD5 hash
			var inputBytes = Encoding.UTF8.GetBytes(value.ToString());
			var hashBytes = md5.ComputeHash(inputBytes);
				
			// Convert the byte array to hexadecimal string
			return hashBytes
				.Aggregate(new StringBuilder(), (sb, currByte) => sb.Append(currByte.ToString("X2")))
				.ToString();
		}
	}
}