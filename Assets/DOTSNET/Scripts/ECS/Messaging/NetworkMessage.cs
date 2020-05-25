// NetworkMessage is an interface so that messages can be structs
// (in order to avoid allocations)
namespace DOTSNET
{
	public interface NetworkMessage
	{
		// messages need an id. we assign it manually so that it's easier to
		// debug, instead of hashing the type name by default, which is hard to
		// debug.
		// => this makes it easier to communicate with external applications too
	    // => name hashing is still supported if needed, by returning the hash
		ushort GetID();

	    // OnSerialize serializes a message via SegmentWriter.
	    // returns false if buffer was too small for all the data, or if it
	    // contained invalid data (e.g. from an attacker).
	    bool Serialize(ref SegmentWriter writer);

	    // OnDeserialize deserializes a message via SegmentReader.
	    // returns false if buffer was too small for all the data, or if it
	    // contained invalid data (e.g. from an attacker).
	    bool Deserialize(ref SegmentReader reader);
	}
}