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
    public class LanguageText
    {
        public string CultureName { get ; } // ISO culture code describes the language in a portable way. Nether null
        public string ShortDesc;
        public string LongDesc;
        public string XDesc;
        public string Comment;

        public LanguageText(lang_t viLang, Language voLang)
        {
            string s;
            if (voLang.TryGetISO(viLang, out s)) CultureName = s; else throw new KeyNotFoundException($"Language {viLang} has no associated culture");
        }

        public LanguageText(string vsCultureName, Language voLang)
        {
            if (vsCultureName == null) throw new ArgumentNullException("Culture name");
            lang_t iLang;
            if (!voLang.TryGetLanguageID(vsCultureName, out iLang)) throw new KeyNotFoundException($"No language has the {vsCultureName} culture");
        }
    }


    public enum LanguageMasks
    {
        ShortDesc = 1,
        LongDesc = 2,
        XDesc = 4,
        Comment = 8,
        All = 15,
        None = 0
    }


    // Loads and caches all languages descriptions
    //      Maps FC languages and ISO culture codes that can be used by clients through the en-US description of the language in FC
    //      Sets a working language and its culture:
    //              - if the client culture is found in an active FC language, use it
    //              - otherwise use the first active language for FC descriptions, and en-US as culture for the user interface

    public class Language
    {
        #region static class fields used to handle FC enumerations
        private static readonly log4net.ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static int[] _aiDescProps = {
            (int)ct_object_property.CT_SDESC1_PROP,
            (int)ct_object_property.CT_SDESC2_PROP,
            (int)ct_object_property.CT_SDESC3_PROP,
            (int)ct_object_property.CT_SDESC4_PROP,
            (int)ct_object_property.CT_SDESC5_PROP,
            (int)ct_object_property.CT_SDESC6_PROP,
            (int)ct_object_property.CT_LDESC1_PROP,
            (int)ct_object_property.CT_LDESC2_PROP,
            (int)ct_object_property.CT_LDESC3_PROP,
            (int)ct_object_property.CT_LDESC4_PROP,
            (int)ct_object_property.CT_LDESC5_PROP,
            (int)ct_object_property.CT_LDESC6_PROP,
            (int)ct_object_property.CT_XDESC1_PROP,
            (int)ct_object_property.CT_XDESC2_PROP,
            (int)ct_object_property.CT_XDESC3_PROP,
            (int)ct_object_property.CT_XDESC4_PROP,
            (int)ct_object_property.CT_XDESC5_PROP,
            (int)ct_object_property.CT_XDESC6_PROP,
            (int)ct_object_property.CT_CDESC1_PROP,
            (int)ct_object_property.CT_CDESC2_PROP,
            (int)ct_object_property.CT_CDESC3_PROP,
            (int)ct_object_property.CT_CDESC4_PROP,
            (int)ct_object_property.CT_CDESC5_PROP,
            (int)ct_object_property.CT_CDESC6_PROP,
        };
        private static lang_t[] _aiIndex2LangIds = {
            CTCLIENTSERVERLib.lang_t.lang_1,
            CTCLIENTSERVERLib.lang_t.lang_2,
            CTCLIENTSERVERLib.lang_t.lang_3,
            CTCLIENTSERVERLib.lang_t.lang_4,
            CTCLIENTSERVERLib.lang_t.lang_5,
            CTCLIENTSERVERLib.lang_t.lang_6,
        };

        private static Dictionary<lang_t, int> _oLangId2Index = new Dictionary<lang_t, int>()
        {
            { CTCLIENTSERVERLib.lang_t.lang_1, 0 },
            { CTCLIENTSERVERLib.lang_t.lang_2, 1 },
            { CTCLIENTSERVERLib.lang_t.lang_3, 2 },
            { CTCLIENTSERVERLib.lang_t.lang_4, 3 },
            { CTCLIENTSERVERLib.lang_t.lang_5, 4 },
            { CTCLIENTSERVERLib.lang_t.lang_6, 5 }
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

        private static int PrLang2Index(lang_t viLang)
        {
            int i;
            if (!_oLangId2Index.TryGetValue(viLang, out i)) throw new ArgumentOutOfRangeException($"Unknown language {viLang}");
            return i;
        }
        #endregion

        #region private instance fields
        // Retrieves a lang_t from a recognized ISO culture ID (plus Local just in case)
        private readonly Dictionary<string, lang_t> _oISO2Lang = new Dictionary<string, lang_t>();
        private readonly Dictionary<lang_t, string> _oLang2ISO = new Dictionary<lang_t, string>();
        private readonly List<lang_t> _oActiveLanguages = new List<lang_t>();
        private readonly string[,] _asDescs = new string[6, 6];

        private string PrGetDesc(lang_t viLang) => _asDescs[PrLang2Index(viLang), PrLang2Index(WorkingLanguage)];

        private (lang_t, string) PrIdentifyLanguages(ConfigClass roConfig, string vsRequestedWorkingLanguageISO)
        {
            lang_t iWorkingLanguage;
            string sActiveWorkingLanguageISO;

            ICtProviderContainer providerContainer = (ICtProviderContainer)roConfig.Session;
            ICtObjectManager langManager = (ICtObjectManager)providerContainer.get_Provider(1, (int)CTCLIENTSERVERLib.CT_CLIENTSERVER_MANAGERS.CT_LANGUAGE_MANAGER);
            CTCLIENTSERVERLib.ICtGenCollection langCol = langManager.GetObjects(null, ACCESSFLAGS.OM_READ, (int)ALL_CAT.ALL, null);

            int c = 0;
            int j;
            string sLangDesc;
            string sLangISO;
            bool bActive;
            bool bISOFound;
            foreach (object oLang in langCol)
            {
                bActive = ((ICtLanguage)oLang).IsActive;
                if (bActive) _oActiveLanguages.Add(_aiIndex2LangIds[c]);
                j = 0;
                bISOFound = false;
                foreach (lang_t iLang in _aiIndex2LangIds)
                {
                    sLangDesc = Description((ICtLanguage)oLang, ct_desctype.ctdesc_long, iLang);
                    _asDescs[c, j] = sLangDesc;
                    // If the same description is used on multiple rows, keep the first and silently ignore the others
                    if (bActive && !bISOFound && _oDesc2ISO.TryGetValue(sLangDesc, out sLangISO) && !_oISO2Lang.ContainsKey(sLangISO))
                    {
                        bISOFound = true;
                        _oISO2Lang.Add(sLangISO, _aiIndex2LangIds[c]);
                        if (!_oLang2ISO.ContainsKey(_aiIndex2LangIds[c])) _oLang2ISO.Add(_aiIndex2LangIds[c], sLangISO);
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
                if (!_oLang2ISO.TryGetValue(iWorkingLanguage, out sActiveWorkingLanguageISO)) sActiveWorkingLanguageISO = "en-US";
            }
            return (iWorkingLanguage, sActiveWorkingLanguageISO);
        }
        #endregion

        // Desc[ct_desctype, lang_t] crashes when returning a null value in a multi thread setting. Use Prop instead
        public static string Description(ICtObjectBase roFCObj, ct_desctype viType, lang_t viLang)
        {
            const int cLang = 6;

            int iLangOffset = PrLang2Index(viLang);
            switch (viType)
            {
                case ct_desctype.ctdesc_short:
                    break;
                case ct_desctype.ctdesc_long:
                    iLangOffset += cLang;
                    break;
                case ct_desctype.ctdesc_extralong:
                    iLangOffset += 2 * cLang;
                    break;
                case ct_desctype.ctdesc_comment:
                    iLangOffset += 3 * cLang;
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown description type {viType}");
            }
            return (string)(roFCObj?.PropVal[_aiDescProps[iLangOffset]]);
        }


        public readonly lang_t WorkingLanguage;

        public readonly CultureInfo Culture;

        public readonly List<(lang_t, string)> SupportedLanguages;

        public Language(ConfigClass roConfig, string vsRequestedWorkingLanguageISO, string vsRequestedSupportedLanguagesISO)
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

            SupportedLanguages = new List<(lang_t, string)>();
            if (vsRequestedSupportedLanguagesISO != null)
            {
                lang_t iLang;
                foreach (string s in vsRequestedSupportedLanguagesISO.Split(new char[] { ' ', ',', ';' })){
                    if (_oISO2Lang.TryGetValue(s, out iLang) && _oActiveLanguages.Contains(iLang)) SupportedLanguages.Add((iLang, s));
                }
            } 
            else
            {
                SupportedLanguages.Add((WorkingLanguage, sActiveWorkingLanguageISO));
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

        public bool TryGetISO(lang_t viLang, out string roCultureName)
        {
            return (_oLang2ISO.TryGetValue(viLang, out roCultureName)) ? true : (null == (roCultureName = null)); ;
        }

        public bool TryGetLanguageID(string vsCultureName, out lang_t riLang)
        {
            return (_oISO2Lang.TryGetValue(vsCultureName, out riLang)) ? true : (0 == (riLang = 0));
        }
    }
}