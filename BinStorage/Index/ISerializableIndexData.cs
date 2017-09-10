namespace BinStorage.Index
{
    /// <summary>
    /// Provides common interface for data serializable for index
    /// </summary>
    internal interface ISerializableIndexData
    {
        /// <summary>
        /// Gets size of serialized representation of the object 
        /// </summary>
        /// <remarks>
        /// Value should be known before serialization and after deserialization
        /// </remarks>
        int SerializedLength { get; }

        /// <summary>
        /// Deserializes data from given buffer starting at specified offset
        /// </summary>
        /// <param name="buffer">buffer with serialized data</param>
        /// <param name="offset">offset within the buffer where object data starts</param>
        void Deserialize(byte[] buffer, int offset);

        /// <summary>
        /// Serializes data into provided buffer starting at specified offset
        /// </summary>
        /// <param name="buffer">buffer to write serialized data to</param>
        /// <param name="offset">offset within the buffer to start writing from</param>
        void Serialize(byte[] buffer, int offset);
    }
}