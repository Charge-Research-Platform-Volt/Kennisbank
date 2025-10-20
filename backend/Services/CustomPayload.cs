using Google.Protobuf.Collections;
using Qdrant.Client.Grpc;

namespace KnowledgeBank.Models
{
    public class CustomPayload
    {
        public string ResourceId { get; set; } = string.Empty;
        public string ChunkType { get; set; } = string.Empty;
        public string ChunkText { get; set; } = string.Empty;
        public int ChunkPart { get; set; }



        /// <summary>
        /// Creates a CustomPayload instance from a MapField containing string-Value pairs.
        /// </summary>
        /// <param name="payload">A MapField containing the payload data with keys: resourceId, chunkType, chunkText, and chunkPart</param>
        /// <returns>A new CustomPayload instance populated with values from the payload, using default values for missing keys</returns>
        /// <remarks>
        /// Default values are applied when keys are missing:
        /// - resourceId: empty string
        /// - chunkType: empty string  
        /// - chunkText: empty string
        /// - chunkPart: -1
        /// </remarks>
        public static CustomPayload FromPayload(MapField<string, Value> payload)
        {
            return new CustomPayload
            {
                ResourceId = payload.GetValueOrDefault("resourceId", new Value { StringValue = "" }).StringValue,
                ChunkType = payload.GetValueOrDefault("chunkType", new Value { StringValue = "" }).StringValue,
                ChunkText = payload.GetValueOrDefault("chunkText", new Value { StringValue = "" }).StringValue,
                ChunkPart = (int)payload.GetValueOrDefault("chunkPart", new Value { IntegerValue = -1 }).IntegerValue
            };
        }



        /// <summary>
        /// Converts the current object to a dictionary payload format suitable for serialization or API communication.
        /// </summary>
        /// <returns>
        /// A dictionary containing the object's properties as Value objects:
        /// - "resourceId": String value containing the resource identifier
        /// - "chunkType": String value indicating the type of chunk
        /// - "chunkText": String value containing the actual chunk text content
        /// - "chunkPart": Integer value representing the chunk part number
        /// </returns>
        public Dictionary<string, Value> ToPayload()
        {
            return new Dictionary<string, Value>
            {
                ["resourceId"] = new Value { StringValue = ResourceId },
                ["chunkType"] = new Value { StringValue = ChunkType },
                ["chunkText"] = new Value { StringValue = ChunkText },
                ["chunkPart"] = new Value { IntegerValue = ChunkPart }
            };
        }
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


