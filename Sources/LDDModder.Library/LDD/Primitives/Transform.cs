﻿using LDDModder.Simple3D;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LDDModder.LDD.Primitives
{
    public class Transform
    {
        public float Angle { get; set; }
        public Vector3 Axis { get; set; }
        public Vector3 Translation { get; set; }

        public XAttribute[] ToXmlAttributes()
        {
            return new XAttribute[]
            {
                new XAttribute("angle", Angle.ToString(NumberFormatInfo.InvariantInfo)),
                new XAttribute("ax", Axis.X.ToString(NumberFormatInfo.InvariantInfo)),
                new XAttribute("ay", Axis.Y.ToString(NumberFormatInfo.InvariantInfo)),
                new XAttribute("az", Axis.Z.ToString(NumberFormatInfo.InvariantInfo)),
                new XAttribute("tx", Translation.X.ToString(NumberFormatInfo.InvariantInfo)),
                new XAttribute("ty", Translation.Y.ToString(NumberFormatInfo.InvariantInfo)),
                new XAttribute("tz", Translation.Z.ToString(NumberFormatInfo.InvariantInfo))
            };
        }

        public static Transform FromElementAttributes(XElement element)
        {
            return new Transform
            {
                Angle = element.ReadAttribute<float>("angle"),
                Axis = new Vector3(
                    element.ReadAttribute<float>("ax"),
                    element.ReadAttribute<float>("ay"),
                    element.ReadAttribute<float>("az")),
                Translation = new Vector3(
                    element.ReadAttribute<float>("tx"),
                    element.ReadAttribute<float>("ty"),
                    element.ReadAttribute<float>("tz")),
            };
        }
    }
}