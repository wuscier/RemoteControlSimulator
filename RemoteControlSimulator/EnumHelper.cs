﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RemoteControlSimulator
{
    public static class EnumHelper
    {
        private const string _unknown = "UNKNOWN";
        public static string GetName(Type t, object v)
        {
            try
            {
                return Enum.GetName(t, v);
            }
            catch (Exception)
            {
                return _unknown;
            }
        }

        public static string GetDescription(Type t, object v)
        {
            try
            {
                FieldInfo fieldInfo = t.GetField(GetName(t, v));

                DescriptionAttribute[] attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
                return attributes.Length > 0 ? attributes[0].Description : GetName(t, v);
            }
            catch (Exception)
            {
                return _unknown;
            }
        }
    }
}
