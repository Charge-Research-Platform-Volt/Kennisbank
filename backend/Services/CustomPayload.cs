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