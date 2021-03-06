﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LDDModder.PaletteMaker.Rebrickable
{
    public static class RebrickableAPI
    {
        internal const string API_KEY = "aU49o5xulf";
        internal const string API_URL = "https://rebrickable.com/api/";
        internal static WebClient WC;

        public static readonly ApiFunction<GetPartParameters, PartInfo> GetPart;
        public static readonly ApiFunction<GetSetPartsParameters, SetParts> GetSetParts;

        static RebrickableAPI()
        {
            WC = new WebClient() { Proxy = null };
            GetPart = new ApiFunction<GetPartParameters, PartInfo>("get_part");
            GetSetParts = new ApiFunction<GetSetPartsParameters, SetParts>("get_set_parts");
            //GetSet = new ApiFunction<GetSetParameters, RBSet>("get_set");
            //GetElement = new ApiFunction<GetElementParameters, RBElement>("get_element");
        }

        internal static byte[] DownloadWebPage(string url)
        {
            return WC.DownloadData(url);
        }
    }
}
