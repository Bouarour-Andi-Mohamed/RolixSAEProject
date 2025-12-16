using Microsoft.Extensions.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using RolixSAEProject.Models;
using System;
using System.Linq;

namespace RolixSAEProject.Services
{
    public class SiteContentService
    {
        private readonly ServiceClient _client;

        public SiteContentService(IConfiguration configuration)
        {
            var url = configuration["Dataverse:Url"];

            var connStr =
                $"AuthType=OAuth;" +
                $"Url={url};" +
                $"AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;" +
                $"RedirectUri=http://localhost;" +
                $"LoginPrompt=Auto;";

            _client = new ServiceClient(connStr);
        }

        // =========================================================
        // TABLE + CHAMPS (NOMS LOGIQUES DATAVERSE)
        // =========================================================

        private const string TablePagesDuSite = "crda6_siteweb";

        // Identité
        private const string ColCleDePage = "crda6_cledepage";

        // Images (URL)
        private const string ColImageBanniereUrl = "crda6_imagebanniere";
        private const string ColImageHorlogerieUrl = "crda6_imagehorlogerie";

        // Hero + Titre page
        private const string ColTitreHeroFR = "crda6_titreherofr";
        private const string ColTitreHeroEN = "crda6_titreheroen";
        private const string ColTitrePageFR = "crda6_titrepagefr";
        private const string ColTitrePageEN = "crda6_titrepageen";

        // Histoire
        private const string ColTitreHistoireFR = "crda6_titrehistoirefr";
        private const string ColTitreHistoireEN = "crda6_titrehistoireen";
        private const string ColHistoireP1FR = "crda6_paragraphehistoire1fr";
        private const string ColHistoireP1EN = "crda6_paragraphehistoire1en";
        private const string ColHistoireP2FR = "crda6_paragraphehistoire2fr";
        private const string ColHistoireP2EN = "crda6_paragraphehistoire2en";

        // Valeurs (titre section)
        private const string ColTitreValeursFR = "crda6_titrevaleursfr";
        private const string ColTitreValeursEN = "crda6_titrevaleursen";

        // Valeur 1
        private const string ColValeur1TitreFR = "crda6_valeur1titrefr";
        private const string ColValeur1TitreEN = "crda6_valeur1titreen";
        private const string ColValeur1TexteFR = "crda6_valeur1textefr";
        private const string ColValeur1TexteEN = "crda6_valeur1texteen";

        // Valeur 2
        private const string ColValeur2TitreFR = "crda6_valeur2titrefr";
        private const string ColValeur2TitreEN = "crda6_valeur2titreen";
        private const string ColValeur2TexteFR = "crda6_valeur2textefr";
        private const string ColValeur2TexteEN = "crda6_valeur2texteen";

        // Valeur 3
        private const string ColValeur3TitreFR = "crda6_valeur3titrefr";
        private const string ColValeur3TitreEN = "crda6_valeur3titreen";
        private const string ColValeur3TexteFR = "crda6_valeur3textefr";
        private const string ColValeur3TexteEN = "crda6_valeur3texteen";

        // Art de l’horlogerie
        private const string ColTitreArtFR = "crda6_titreartfr";
        private const string ColTitreArtEN = "crda6_titrearten";
        private const string ColArtP1FR = "crda6_paragrapheart1fr";
        private const string ColArtP1EN = "crda6_paragrapheart1en";
        private const string ColArtP2FR = "crda6_paragrapheart2fr";
        private const string ColArtP2EN = "crda6_paragrapheart2en";

        // Engagement
        private const string ColTitreEngagementFR = "crda6_titreengagementfr";
        private const string ColTitreEngagementEN = "crda6_titreengagementen";
        private const string ColEngagementTexteFR = "crda6_texteengagementfr";
        private const string ColEngagementTexteEN = "crda6_texteengagementen";

        // =========================================================
        // Helpers
        // =========================================================
        private static string? GetString(Entity e, string attributeLogicalName)
            => e.GetAttributeValue<string>(attributeLogicalName);

        // =========================================================
        // Public
        // =========================================================
        public AboutUsPage? GetAboutUsPage(string pageKey = "about-us")
        {
            if (_client?.IsReady != true)
                return null;

            var query = new QueryExpression(TablePagesDuSite)
            {
                ColumnSet = new ColumnSet(
                    ColCleDePage,
                    ColImageBanniereUrl,
                    ColImageHorlogerieUrl,

                    ColTitreHeroFR, ColTitreHeroEN,
                    ColTitrePageFR, ColTitrePageEN,

                    ColTitreHistoireFR, ColTitreHistoireEN,
                    ColHistoireP1FR, ColHistoireP1EN,
                    ColHistoireP2FR, ColHistoireP2EN,

                    ColTitreValeursFR, ColTitreValeursEN,

                    ColValeur1TitreFR, ColValeur1TitreEN, ColValeur1TexteFR, ColValeur1TexteEN,
                    ColValeur2TitreFR, ColValeur2TitreEN, ColValeur2TexteFR, ColValeur2TexteEN,
                    ColValeur3TitreFR, ColValeur3TitreEN, ColValeur3TexteFR, ColValeur3TexteEN,

                    ColTitreArtFR, ColTitreArtEN,
                    ColArtP1FR, ColArtP1EN,
                    ColArtP2FR, ColArtP2EN,

                    ColTitreEngagementFR, ColTitreEngagementEN,
                    ColEngagementTexteFR, ColEngagementTexteEN
                ),
                Criteria =
                {
                    Conditions =
                    {
                        new ConditionExpression(ColCleDePage, ConditionOperator.Equal, pageKey)
                    }
                },
                TopCount = 1
            };

            Entity? entity;
            try
            {
                entity = _client.RetrieveMultiple(query).Entities.FirstOrDefault();
            }
            catch
            {
                return null;
            }

            if (entity == null)
                return null;

            return new AboutUsPage
            {
                CleDePage = GetString(entity, ColCleDePage),

                ImageBanniereUrl = GetString(entity, ColImageBanniereUrl),
                ImageHorlogerieUrl = GetString(entity, ColImageHorlogerieUrl),

                TitreHeroFR = GetString(entity, ColTitreHeroFR),
                TitreHeroEN = GetString(entity, ColTitreHeroEN),
                TitrePageFR = GetString(entity, ColTitrePageFR),
                TitrePageEN = GetString(entity, ColTitrePageEN),

                TitreHistoireFR = GetString(entity, ColTitreHistoireFR),
                TitreHistoireEN = GetString(entity, ColTitreHistoireEN),
                HistoireP1FR = GetString(entity, ColHistoireP1FR),
                HistoireP1EN = GetString(entity, ColHistoireP1EN),
                HistoireP2FR = GetString(entity, ColHistoireP2FR),
                HistoireP2EN = GetString(entity, ColHistoireP2EN),

                TitreValeursFR = GetString(entity, ColTitreValeursFR),
                TitreValeursEN = GetString(entity, ColTitreValeursEN),

                Valeur1TitreFR = GetString(entity, ColValeur1TitreFR),
                Valeur1TitreEN = GetString(entity, ColValeur1TitreEN),
                Valeur1TexteFR = GetString(entity, ColValeur1TexteFR),
                Valeur1TexteEN = GetString(entity, ColValeur1TexteEN),

                Valeur2TitreFR = GetString(entity, ColValeur2TitreFR),
                Valeur2TitreEN = GetString(entity, ColValeur2TitreEN),
                Valeur2TexteFR = GetString(entity, ColValeur2TexteFR),
                Valeur2TexteEN = GetString(entity, ColValeur2TexteEN),

                Valeur3TitreFR = GetString(entity, ColValeur3TitreFR),
                Valeur3TitreEN = GetString(entity, ColValeur3TitreEN),
                Valeur3TexteFR = GetString(entity, ColValeur3TexteFR),
                Valeur3TexteEN = GetString(entity, ColValeur3TexteEN),

                TitreArtFR = GetString(entity, ColTitreArtFR),
                TitreArtEN = GetString(entity, ColTitreArtEN),
                ArtP1FR = GetString(entity, ColArtP1FR),
                ArtP1EN = GetString(entity, ColArtP1EN),
                ArtP2FR = GetString(entity, ColArtP2FR),
                ArtP2EN = GetString(entity, ColArtP2EN),

                TitreEngagementFR = GetString(entity, ColTitreEngagementFR),
                TitreEngagementEN = GetString(entity, ColTitreEngagementEN),
                EngagementTexteFR = GetString(entity, ColEngagementTexteFR),
                EngagementTexteEN = GetString(entity, ColEngagementTexteEN),
            };
        }
    }
}
