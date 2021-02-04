#region Copyright
// ----------------------- IMPORTANT - READ CAREFULLY: COPYRIGHT NOTICE -------------------
// -- THIS SOFTWARE IS THE PROPERTY OF CTStation S.A.S. IN ANY COUNTRY                   --
// -- (WWW.CTSTATION.NET). ANY COPY, CHANGE OR DERIVATIVE WORK                           --
// -- IS SUBJECT TO CTSTATION S.A.S.’S PRIOR WRITTEN CONSENT.                            --
// -- THIS SOFTWARE IS REGISTERED TO THE FRENCH ANTI-PIRACY AGENCY (APP).                --
// -- COPYRIGHT 2020-01 CTSTATTION S.A.S. – ALL RIGHTS RESERVED.                         --
// ----------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Globalization;
using log4net;
using CTCLIENTSERVERLib;

namespace CTSWeb.Util
{
    // Loads and caches all languages descriptions
    //      Maps FC languages and ISO culture codes that can be used by clients through the en-US description of the language in FC
    //      Sets a working language and its culture:
    //              - if the client culture is found in an active FC language, use it
    //              - otherwise use the first active language for FC descriptions, and en-US as culture for the user interface

    public class Language
    {
        #region static class fields used to handle FC enumerations
        private static readonly log4net.ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static int[] _aiLDescProps = {
            (int)ct_object_property.CT_LDESC1_PROP,
            (int)ct_object_property.CT_LDESC2_PROP,
            (int)ct_object_property.CT_LDESC3_PROP,
            (int)ct_object_property.CT_LDESC4_PROP,
            (int)ct_object_property.CT_LDESC5_PROP,
            (int)ct_object_property.CT_LDESC6_PROP,
        };

        private static lang_t[] _aiCtLangIds = {
            CTCLIENTSERVERLib.lang_t.lang_1,
            CTCLIENTSERVERLib.lang_t.lang_2,
            CTCLIENTSERVERLib.lang_t.lang_3,
            CTCLIENTSERVERLib.lang_t.lang_4,
            CTCLIENTSERVERLib.lang_t.lang_5,
            CTCLIENTSERVERLib.lang_t.lang_6,
        };

        private static Dictionary<string, string> _oDesc2ISO = new Dictionary<string, string>()
        {
            {"English", "en-US"},
            {"French", "fr-FR"},
            {"German", "de-DE"},
            {"Spanish", "es-ES"},
            {"Japanese", "ja-JP"},
            {"Hebrew", "he-IL"},
            {"Local", "Local" }
        };

        private static int PrLangIndex(lang_t viLang)
        {
            int c = 0;
            foreach (lang_t iCur in _aiCtLangIds)
            {
                if (iCur == viLang) return c;
                c++;
            }
            throw new ArgumentOutOfRangeException($"Unknown language {viLang}");
        }
        #endregion

        #region private instance fields
        // Retrieves a lang_t from a recognized ISO culture ID (plus Local just in case)
        private readonly Dictionary<string, lang_t> _oISO2Lang = new Dictionary<string, lang_t>();
        private readonly List<lang_t> _oActiveLanguages = new List<lang_t>();
        private readonly string[,] _asDescs = new string[6, 6];

        private string PrGetDesc(lang_t viLang) => _asDescs[PrLangIndex(viLang), PrLangIndex(WorkingLanguage)];

        private (lang_t, string) PrIdentifyLanguages(ConfigClass roConfig, string vsRequestedWorkingLanguageISO)
        {
            lang_t iWorkingLanguage;
            string sActiveWorkingLanguageISO;

            ICtProviderContainer providerContainer = (ICtProviderContainer)roConfig.Session;
            ICtObjectManager langManager = (ICtObjectManager)providerContainer.get_Provider(1, (int)CTCLIENTSERVERLib.CT_CLIENTSERVER_MANAGERS.CT_LANGUAGE_MANAGER);
            CTCLIENTSERVERLib.ICtGenCollection langCol = langManager.GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null);

            Dictionary<lang_t, string> oLang2ISO = new Dictionary<lang_t, string>();           
            int c = 0;
            int j;
            string sLangDesc;
            string sLangISO;
            bool bActive;
            bool bISOFound;
            foreach (object oLang in langCol)
            {
                bActive = ((ICtLanguage)oLang).IsActive;
                if (bActive) _oActiveLanguages.Add(_aiCtLangIds[c]);
                j = 0;
                bISOFound = false;
                foreach (int iDesc in _aiLDescProps)
                {
                    sLangDesc = (string)((ICtLanguage)oLang).PropVal[iDesc];
                    _asDescs[c, j] = sLangDesc;
                    // If the same description is used on multiple rows, keep the first and silently ignore the others
                    if (bActive && !bISOFound && _oDesc2ISO.TryGetValue(sLangDesc, out sLangISO) && !_oISO2Lang.ContainsKey(sLangISO))
                    {
                        bISOFound = true;
                        _oISO2Lang.Add(sLangISO, _aiCtLangIds[c]);
                        if (!oLang2ISO.ContainsKey(_aiCtLangIds[c])) oLang2ISO.Add(_aiCtLangIds[c], sLangISO);
                    }
                    j++;
                }
                c++;
            }
            if (_oActiveLanguages.Count == 0) throw new Exception("No active language found");
            if (!(vsRequestedWorkingLanguageISO == null) && _oISO2Lang.TryGetValue(vsRequestedWorkingLanguageISO, out iWorkingLanguage))
            {
                sActiveWorkingLanguageISO = vsRequestedWorkingLanguageISO;
            } 
            else
            {
                iWorkingLanguage = _oActiveLanguages[0];
                if (!oLang2ISO.TryGetValue(iWorkingLanguage, out sActiveWorkingLanguageISO)) sActiveWorkingLanguageISO = "en-US";
            }
            return (iWorkingLanguage, sActiveWorkingLanguageISO);
        }
#endregion

        public lang_t WorkingLanguage { get ; }
        
        public CultureInfo Culture { get ;  }

        public Language(ConfigClass roConfig, string vsRequestedWorkingLanguageISO)
        {
            string sActiveWorkingLanguageISO;

            (WorkingLanguage, sActiveWorkingLanguageISO) = PrIdentifyLanguages(roConfig, vsRequestedWorkingLanguageISO);
            try
            {
                Culture = new CultureInfo(sActiveWorkingLanguageISO);
            } 
            catch (CultureNotFoundException e)
            {
                _oLog.Debug(e.Message);
                Culture = new CultureInfo("en-US");
            }
        }

        public List<String> GetActiveLanguages()
        {
            List<string> oRet = new List<string>();

            foreach (lang_t iLang in _oActiveLanguages)
            {
                oRet.Add(PrGetDesc(iLang));
            }
            return oRet;
        }

        public List<String> GetKnownCultureLanguages()
        {
            List<string> oRet = new List<string>();

            foreach (lang_t iLang in _oISO2Lang.Values)
            {
                oRet.Add(PrGetDesc(iLang));
            }
            return oRet;
        }
    }
}