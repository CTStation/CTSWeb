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
    // TODO: transform the message initialization code into a fair data structure in the static constructor (see Manager)
    public static class MessageSetup
    {
        public static readonly List<(string, MessageSeverity, ((string, string, string), (string, string, string)))> Setup = new List<(string, MessageSeverity, ((string, string, string), (string, string, string)))>()
        {
           ("RF0010",MessageSeverity.Error, ( ("en-US", "No framework found for category {0}, version {1}", "Choose another category or validate a category builder"),
                                              ("fr-FR", "Auncun référentiel pour la phase {0}, version {1}", "Choisissez une autre phase ou validez un référentiel de la phase") )
            ),
           ("RF0011",MessageSeverity.Error, ( ("en-US", "No published framework found for category {0}, , version {1}", "Choose another category or publish the category builder"),
                                              ("fr-FR", "Auncun référentiel publié pour la phase {0}, version {1}", "Choisissez une autre phase ou publiez le référentiel") )
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
           ("RF0213",MessageSeverity.Error, ( ("en-US", "Code '{1}' not found in dimension {0}", ""),
                                              ("fr-FR", "Code '{1}' inconnu dans la dimension {0}", "") )
            ),

           ("RF0310",MessageSeverity.Info, ( ("en-US", "Changed {0} from '{1}' to '{2}'", ""),
                                              ("fr-FR", "Change {0} de '{1}' à '{2}'", "") )
            ),
           ("RF0311",MessageSeverity.Info, ( ("en-US", "Object of type {0} named {1} is not judged valid by FC", "Please check its parameters"),
                                              ("fr-FR", "L'objet de type {0} et de code {1} n'est pas considéré comme valide pas FC", "Vérifiez les paramètres") )
            ),
           ("RF0312",MessageSeverity.Info, ( ("en-US", "Error {0} while saving object {1} of type {2}", ""),
                                              ("fr-FR", "Erreur {0} lors de la sauvegarde de l'objet {1} de type {2}", "") )
            ),
           ("RF0410",MessageSeverity.Error, ( ("en-US", "A new object of type {0} must have a name", "Make sure a name is provided"),
                                              ("fr-FR", "Un nouvel object de type {0} doit avoir un code", "Fournissez un code pour l'objet") )
            ),
           ("RF0411",MessageSeverity.Error, ( ("en-US", "An object of name '{0}' already exists in type {1}", "Choose another name that isn't used by another existing obejct"),
                                              ("fr-FR", "Un object de code '{0}' et de type {1] existe déjà", "Choisissez un autre nom, qui ne soit pas utilisé par un objet existant") )
            ),
           ("RF0412",MessageSeverity.Error, ( ("en-US", "Character '{0}' is not allowed in the name '{1}'", "Change the name to use only letters, digits and -"),
                                              ("fr-FR", "Le caracter '{0}' n'est pas accepté dans le code '{1}'", "Changez le code en utilisant uniquement des lettres, des chiffres ou le -") )
            ),
           ("RF0413",MessageSeverity.Error, ( ("en-US", "Name can't be empty", "Change the code"),
                                              ("fr-FR", "Un code ne doit pas être vide", "Changez le code") )
            ),
           ("RF0414",MessageSeverity.Error, ( ("en-US", "The code '{0}' is too long", "Limit the code to 12 characters"),
                                              ("fr-FR", "Le code '{0}' est trop long", "Limitez le code à 12 caractères") )
            ),
           ("RF0415",MessageSeverity.Error, ( ("en-US", "No object of type {0} has ID {1}", "The object may have been deleted. Please try again"),
                                              ("fr-FR", "Aucun objet de type {0} n'a l'ID {1}", "Cet objet a peut-être été supprimé. Recommancez l'opération") )
            ),
           ("RF0450",MessageSeverity.Error, ( ("en-US", "Can't get a new object of type {0}", "The server may be saturated. Please disconnect, reconnect, and try again"),
                                              ("fr-FR", "Impossible d'obtenir un nouvel objet de type {0}", "Le serveur peut être saturé. Quittez l'application et essayez une autre fois") )
            ),
           ("RF0451",MessageSeverity.Error, ( ("en-US", "Can't open object '{0}' of type {1} for writing", "Object may be edited by another FC session"),
                                              ("fr-FR", "Impossible d'ouvrir l'objet '{0}' de type {1} en écriture", "L'objet est peut-être en cours d'édition dans une autre session FC") )
            ),
           ("RF0452",MessageSeverity.Error, ( ("en-US", "Errors occured during this session. No modifications will be attempted", "Please reconnect and try again"),
                                              ("fr-FR", "Des erreurs ont eu lieu dans cette session. Les modification sont suspendues", "Reconnectez-vous et essayez une autre fois") )
            ),
           ("RF0510",MessageSeverity.Error, ( ("en-US", "The reporting start date must be before the end date", "Please correct the dates"),
                                              ("fr-FR", "La date de début de reporting doit être avant la date de fin", "Corrigez les dates") )
            ),
           ("RF0511",MessageSeverity.Error, ( ("en-US", "The package deadline date must be between the start and end date", "Please correct the dates"),
                                              ("fr-FR", "La date butoir de remise des liasses doit être entre les dates de début et de fin", "Corrigez les dates") )
            ),
           ("RF0512",MessageSeverity.Error, ( ("en-US", "The package deadline date must be between the start and end date for entity {0}", "Please correct the dates"),
                                              ("fr-FR", "La date butoir de remise des liasses doit être entre les dates de début et de fin pour l'unité {0}", "Corrigez les dates") )
            ),
           ("RF0513",MessageSeverity.Error, ( ("en-US", "In the automatic integration after pubication, a control level must be specified if and only if the advanced publication mode is selected", "Please correct the operation"),
                                              ("fr-FR", "Dans l'intégration automatique après publicaton, un niveau de contrôle doit être spécifié si et seulementsi le mode Avancé est choisi", "Corrigez les paramètres d'exploitation") )
            ),
           ("RF0514",MessageSeverity.Error, ( ("en-US", "In the automatic integration after transfer, a control level must be specified if and only if the advanced publication mode is selected", "Please correct the operation"),
                                              ("fr-FR", "Dans l'intégration automatique après pilotage, un niveau de contrôle doit être spécifié si et seulementsi le mode Avancé est choisi", "Corrigez les paramètres d'exploitation") )
            ),
           ("RF0515",MessageSeverity.Error, ( ("en-US", "Entity {0}: In the automatic integration after pubication, a control level must be specified if and only if the advanced publication mode is selected", "Please correct the operation"),
                                              ("fr-FR", "Enité {0}: Dans l'intégration automatique après publicaton, un niveau de contrôle doit être spécifié si et seulementsi le mode Avancé est choisi", "Corrigez les paramètres d'exploitation") )
            ),
           ("RF0516",MessageSeverity.Error, ( ("en-US", "Entity {0}: In the automatic integration after transfer, a control level must be specified if and only if the advanced publication mode is selected", "Please correct the operation"),
                                              ("fr-FR", "Enité {0}: Dans l'intégration automatique après pilotage, un niveau de contrôle doit être spécifié si et seulementsi le mode Avancé est choisi", "Corrigez les paramètres d'exploitation") )
            ),
           ("RF0517",MessageSeverity.Error, ( ("en-US", "Entity {0}: site '{1}' not found", "Please choose an existing site"),
                                              ("fr-FR", "Enité {0}: site '{1}' inconnu", "Choisissez un site existant") )
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
        private readonly CultureInfo _oCulture;
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