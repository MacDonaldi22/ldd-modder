﻿using LDDModder.Serialization;
using LDDModder.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace LDDModder.LDD.Primitives
{
    [Serializable]
    public abstract class Collision
    {
        internal static string[] AttributeOrder = new string[] { "angle", "ax", "ay", "az", "tx", "ty", "tz" };

        //[XmlAttribute("angle")]
        //public double Angle { get; set; }
        //[XmlAttribute("ax")]
        //public double Ax { get; set; }
        //[XmlAttribute("ay")]
        //public double Ay { get; set; }
        //[XmlAttribute("az")]
        //public double Az { get; set; }
        //[XmlAttribute("tx")]
        //public double Tx { get; set; }
        //[XmlAttribute("ty")]
        //public double Ty { get; set; }
        //[XmlAttribute("tz")]
        //public double Tz { get; set; }

        [XmlIgnore]
        public Transform Transform { get; set; }

        public static IEnumerable<Collision> Deserialize(IEnumerable<XElement> nodes)
        {
            foreach (var node in nodes)
            {
                var result = Deserialize(node);
                if (result != null)
                {
                    var transfo = LDDModder.Serialization.XSerializationHelper.DefaultDeserialize<Transform>(node);
                    result.Transform = transfo;
                    yield return result;
                }
            }
        }

        public static Collision Deserialize(XElement node)
        {
            switch (node.Name.LocalName)
            {
                case "Box":
                    return LDDModder.Serialization.XSerializationHelper.DefaultDeserialize<CollisionBox>(node);
                case "Sphere":
                    return LDDModder.Serialization.XSerializationHelper.DefaultDeserialize<CollisionSphere>(node);
            }
            return null;
        }

        //public static T Deserialize<T>(XElement node)
        //{
        //    var xmlSer = new XmlSerializer(typeof(T));
        //    return (T)xmlSer.Deserialize(node.CreateReader());
        //}

        //public static XElement Serialize(Collision collision)
        //{
        //    //XSerializationHelper.Serialize(
        //    var xmlSer = new XmlSerializer(collision.GetType());
        //    XDocument d = new XDocument();
        //    using (XmlWriter xw = d.CreateWriter())
        //        xmlSer.Serialize(xw, collision);
        //    return d.Root;
        //}

        public static XElement Serialize(Collision collision)
        {
            var result = LDDModder.Serialization.XSerializationHelper.Serialize(collision);
            if (result != null)
            {
                var tranformElem = LDDModder.Serialization.XSerializationHelper.Serialize(collision.Transform);
                foreach (var transformAttr in tranformElem.Attributes())
                    result.Add(transformAttr);
                result.SortAttributes(a => Array.IndexOf(AttributeOrder, a.Name.LocalName));
                return result;
            }
            return null;
        }

        public static IEnumerable<XElement> Serialize(IEnumerable<Collision> collisions)
        {
            foreach (var colObj in collisions)
            {
                var result = LDDModder.Serialization.XSerializationHelper.Serialize(colObj);
                if (result != null)
                {
                    var tranformElem = LDDModder.Serialization.XSerializationHelper.Serialize(colObj.Transform);
                    foreach (var transformAttr in tranformElem.Attributes())
                        result.Add(transformAttr);
                    result.SortAttributes(a => Array.IndexOf(AttributeOrder, a.Name.LocalName));
                    yield return result;
                }
            }
        }
    }
}
