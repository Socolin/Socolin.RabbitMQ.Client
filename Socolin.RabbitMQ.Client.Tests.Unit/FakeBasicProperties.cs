using System.Collections.Generic;
using RabbitMQ.Client;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Socolin.RabbitMQ.Client.Tests.Unit
{
	internal class FakeBasicProperties : IBasicProperties
	{
		public ushort ProtocolClassId { get; set; }
		public string ProtocolClassName { get; set; }

		public void ClearAppId()
		{
			throw new System.NotSupportedException();
		}

		public void ClearClusterId()
		{
			throw new System.NotSupportedException();
		}

		public void ClearContentEncoding()
		{
			throw new System.NotSupportedException();
		}

		public void ClearContentType()
		{
			throw new System.NotSupportedException();
		}

		public void ClearCorrelationId()
		{
			throw new System.NotSupportedException();
		}

		public void ClearDeliveryMode()
		{
			throw new System.NotSupportedException();
		}

		public void ClearExpiration()
		{
			throw new System.NotSupportedException();
		}

		public void ClearHeaders()
		{
			throw new System.NotSupportedException();
		}

		public void ClearMessageId()
		{
			throw new System.NotSupportedException();
		}

		public void ClearPriority()
		{
			throw new System.NotSupportedException();
		}

		public void ClearReplyTo()
		{
			throw new System.NotSupportedException();
		}

		public void ClearTimestamp()
		{
			throw new System.NotSupportedException();
		}

		public void ClearType()
		{
			throw new System.NotSupportedException();
		}

		public void ClearUserId()
		{
			throw new System.NotSupportedException();
		}

		public bool IsAppIdPresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsClusterIdPresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsContentEncodingPresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsContentTypePresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsCorrelationIdPresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsDeliveryModePresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsExpirationPresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsHeadersPresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsMessageIdPresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsPriorityPresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsReplyToPresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsTimestampPresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsTypePresent()
		{
			throw new System.NotSupportedException();
		}

		public bool IsUserIdPresent()
		{
			throw new System.NotSupportedException();
		}

		public string AppId { get; set; }
		public string ClusterId { get; set; }
		public string ContentEncoding { get; set; }
		public string ContentType { get; set; }
		public string CorrelationId { get; set; }
		public byte DeliveryMode { get; set; }
		public string Expiration { get; set; }
		public IDictionary<string, object> Headers { get; set; }
		public string MessageId { get; set; }
		public bool Persistent { get; set; }
		public byte Priority { get; set; }
		public string ReplyTo { get; set; }
		public PublicationAddress ReplyToAddress { get; set; }
		public AmqpTimestamp Timestamp { get; set; }
		public string Type { get; set; }
		public string UserId { get; set; }
	}
}