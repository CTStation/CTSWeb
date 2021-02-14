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
using System.Web;
using System.Threading;
using log4net;


namespace CTSWeb.Util
{
    // Supports a description of a message upon which the end user must act
    //      Similar to build errors or control results
    //
    // Assumes the source data is organized in a table and, when possible, references the error source by 1-based row and column


    // Initialisation does not work with composed class, but does with composed tuples

    public enum MessageSeverity
    {
        Debug = 1,
        Info = 2,
        Warning = 4,
        Error = 8,
        All = 15,
        None = 0
    }


    // Limited to 2 languages, with description that takes arguments and hint to solution that doesn't
    // TODO: static functions to build types for key, multilangual payload and single language payload
    //          i.e. macro $[] in language interpreted as needing translation: the content is interpreted after the format and looked up in a translation table
    //          $[{0}] where 0 = typeof(ReportingLight) would return Reporting.
    //          $[lower:{0}] where 0 = typeof(ReportingLight) would return reporting.
    //          $[lower:AU] would return audit id in english, and nature in french (MessageList needs a list of dimensions for one datasource)
    public static class MessageSetup
    {
        public static readonly List<(string, MessageSeverity, ((string, string, string), (string, string, string)))> Setup = new List<(string, MessageSeverity, ((string, string, string), (string, string, string)))>()
        {
            ( "RF0010", MessageSeverity.Error, ( ("en-US", "Category {0} not found", "Choose another category" ),
                                                ( "fr-FR", "Auncun référentiel validé pour la phase {0}", "Choisissez une autre phase" ) )
            ),
           ("RF0011",MessageSeverity.Error, ( ("en-US", "No validated framework found for category {0}", "Choose another category or validate a category builder"),
                                              ("fr-FR", "Auncun référentiel validé pour la phase {0}", "Choisissez une autre phase ou validez un référentiel de la phase") )
            ),
           ("RF0110",MessageSeverity.Error, ( ("en-US", "Object of type {0} with ID {1} not found", ""),
                                              ("fr-FR", "Object de type {0} avec ID {1} non trouvé", "") )
            ),
           ("RF0111",MessageSeverity.Error, ( ("en-US", "Object of type {0} with name {1} not found", ""),
                                              ("fr-FR", "Object de type {0} de code {1} non trouvé", "") )
            ),
           ("RF0210",MessageSeverity.Error, ( ("en-US", "Table '{0}' not found in data set '{1}'", "Could be a transient network error. Signal the issue if it happens again"),
                                              ("fr-FR", "Table '{0}' non trouvée dans le jeu de données '{1}'", "Peut-être dû à un problème réseau transitoire. Signaler le problème s'il persiste") )
            ),
           ("RF0211",MessageSeverity.Error, ( ("en-US", "Column '{0}' not found in table '{1}'", "Could be a transient network error. Signal the issue if it happens again"),
                                              ("fr-FR", "Colonne '{0}' non trouvée dans la table '{1}'", "Peut-être dû à un problème réseau transitoire. Signaler le problème s'il persiste") )
            ),
           ("RF0212",MessageSeverity.Error, ( ("en-US", "Invalid value '{0}' in column '{1}'", "Correct the entry"),
                                              ("fr-FR", "Valeur incorrecte '{0}' dans la colonne '{1}'", "Corrigez la valeur") )
            ),
        };
    }


    public class Message
    {
        public string Code;
        public MessageSeverity Severity;
        public string Description;
        public int SourceRow;
        public int SourceCol;
    }

    // Reads the setup in one language
    // Builds a list of messages
    // Keeps a list of used setup items and there hints, so that only one copy of used hints is transfered to the client
    public class MessageList
    {
        private static readonly ILog _oLog = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private CultureInfo _oCulture;
        private readonly Dictionary<string, (MessageSeverity, string, string)> _oSetup;

        public List<Message> Messages;
        public Dictionary<string, string> UsedHints;

        public MessageList(string vsCultureName)
        {
            _oCulture = CultureInfo.GetCultureInfo(vsCultureName);     // Throws exception if not known
            string s = _oCulture.Name;

            _oSetup = new Dictionary<string, (MessageSeverity, string, string)>();
            foreach (var oSetupItem in MessageSetup.Setup)
            {
                if (oSetupItem.Item3.Item1.Item1 == s)
                {
                    _oSetup.Add(oSetupItem.Item1, (oSetupItem.Item2, oSetupItem.Item3.Item1.Item2, oSetupItem.Item3.Item1.Item3));
                }
                else if (oSetupItem.Item3.Item2.Item1 == s)
                {
                    _oSetup.Add(oSetupItem.Item1, (oSetupItem.Item2, oSetupItem.Item3.Item2.Item2, oSetupItem.Item3.Item2.Item3));
                }
                else
                {
                    throw new KeyNotFoundException($"No {s} version of message {oSetupItem.Item1} found");
                }
            }
            Messages = new List<Message>();
            UsedHints = new Dictionary<string, string>();
        }

        private Message PrAdd(string vsCode)
        {
            (MessageSeverity, string, string) oSetupItem;
            if (!_oSetup.TryGetValue(vsCode, out oSetupItem)) throw new KeyNotFoundException($"Message {vsCode} not found");
            UsedHints[vsCode] = oSetupItem.Item3;   // Adds if not exists, otherwise update is waisted, by lack of the better TryAdd
            Message oRet = new Message
            {
                Code = vsCode,
                Severity = oSetupItem.Item1,
                Description = oSetupItem.Item2   // Unformatted description
            };
            Messages.Add(oRet);
            return oRet;
        }

        public Message Add(string vsCode) => PrAdd(vsCode);
        public Message Add(string vsCode, object vo1)                          { Message o = PrAdd(vsCode); o.Description = string.Format(_oCulture, o.Description, vo1); return o; }
        public Message Add(string vsCode, object vo1, object vo2)              { Message o = PrAdd(vsCode); o.Description = string.Format(_oCulture, o.Description, vo1, vo2); return o; }
        public Message Add(string vsCode, object vo1, object vo2, object vo3)  { Message o = PrAdd(vsCode); o.Description = string.Format(_oCulture, o.Description, vo1, vo2, vo3); return o; }
        public Message Add(string vsCode, object[] vao)                        { Message o = PrAdd(vsCode); o.Description = string.Format(_oCulture, o.Description, vao); return o; }
    }
}