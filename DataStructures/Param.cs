using System.Xml.Serialization;


namespace HandshakeEmulator.DataStructures
{
    public class Param
    {
        [XmlAttribute]
        public string Name;

        [XmlAttribute]
        public string Suffix;

        public object Value;

        public Param Copy()
        {
            return new Param {Name = Name, Suffix = Suffix, Value = Value};
        }
    }
}
