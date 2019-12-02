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
        public static readonly string[] AttributeNames = new string[] { "angle", "ax", "ay", "az", "tx", "ty", "tz" };

        public float Angle { get; set; }
        public Vector3 Axis { get; set; }
        public Vector3 Translation { get; set; }

        public float Ax { get => Axis.X; set => Axis = new Vector3(value, Axis.Y, Axis.Z); }
        public float Ay { get => Axis.Y; set => Axis = new Vector3(Axis.X, value, Axis.Z); }
        public float Az { get => Axis.Z; set => Axis = new Vector3(Axis.X, Axis.Y, value); }

        public float Tx { get => Translation.X; set => Translation = new Vector3(value, Translation.Y, Translation.Z); }
        public float Ty { get => Translation.Y; set => Translation = new Vector3(Translation.X, value, Translation.Z); }
        public float Tz { get => Translation.Z; set => Translation = new Vector3(Translation.X, Translation.Y, value); }

        public Transform()
        {
            Axis = Vector3.UnitY;
        }

        public Transform(float angle, Vector3 axis, Vector3 translation)
        {
            Angle = angle;
            Axis = axis;
            Translation = translation;
        }

        public Transform(float angle, float ax, float ay, float az, float tx, float ty, float tz)
        {
            Angle = angle;
            Axis = new Vector3(ax, ay, az);
            Translation = new Vector3(tx, ty, tz);
        }

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
                Angle = element.ReadAttribute<float>("angle", 0f),
                Axis = new Vector3(
                    element.ReadAttribute<float>("ax", 0f),
                    element.ReadAttribute<float>("ay", 0f),
                    element.ReadAttribute<float>("az", 0f)),
                Translation = new Vector3(
                    element.ReadAttribute<float>("tx", 0f),
                    element.ReadAttribute<float>("ty", 0f),
                    element.ReadAttribute<float>("tz", 0f)),
            };
        }

        public Matrix4 ToMatrix4()
        {
            var rot = Matrix4.FromAngleAxis(Angle * ((float)Math.PI / 180f), Axis.Normalized());
            var trans = Matrix4.FromTranslation(Translation);
            return rot * trans;
        }

        public Matrix4d ToMatrix4d()
        {
            var rot = Matrix4d.FromAngleAxis(Angle * ((float)Math.PI / 180f), new Vector3d(Axis.X, Axis.Y, Axis.Z).Normalized());
            var trans = Matrix4d.FromTranslation(new Vector3d(Translation.X, Translation.Y, Translation.Z));
            return rot * trans;
        }

        public static Transform FromMatrix(Matrix4 matrix)
        {
            var rot = matrix.ExtractRotation();
            rot.ToAxisAngle(out Vector3 axis, out float angle);
            angle *= 180f / (float)Math.PI;
            return new Transform((float)Math.Round(angle, 4), axis.Rounded(), matrix.ExtractTranslation().Rounded());
        }

        public static Transform FromMatrix(Matrix4d matrix)
        {
            var rot = matrix.ExtractRotation();
            rot.ToAxisAngle(out Vector3d axis, out double angle);
            angle *= 180f / (float)Math.PI;
            return new Transform((float)Math.Round(angle, 4), (Vector3)axis.Rounded(), (Vector3)matrix.ExtractTranslation().Rounded());
        }

        public Vector3 GetPosition()
        {
            return ToMatrix4().TransformPosition(Vector3.Zero);
        }
    }
}
