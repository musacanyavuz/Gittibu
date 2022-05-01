namespace GittiBu.Common
{
    public class Enums
    {
        public enum PaymentRequestStatus
        {
            Bekleniyor = 1,
            KargoyaVerildi = 2,
            Onaylandi = 3,
            SaticiIptalEtti = 4,
            AliciIptalEtti = 5,
            Iptal = 6,
            OnlineOdemeYapildi = 7,
            OtomatikOnaylandi = 8, //Ozhan Atalayla konusuldu burasinin otomatikonaylandi olacagini soyledi. 20.04.2020
            Reddedildi = 9,
            AliciIptalTalebiOlusturdu = 10,
            OdemeAktarildi = 11
        }

        public enum NotificationType
        {
            IlanSatildi = 1,
            IlanKargoBilgisiGuncellemesi = 2,
            IlanIptal = 3,
            SistemMesaji = 4
        }

        public enum DopingGroup
        {
            Goruntulenme,
            Etiket,
            Gorunum,
            Diger,
            AnasayfaBanner,
            LogoBanner,
            HaberKampanya,
            SaticiIthalatci
        }

        public enum ShipmentType
        {
            KargoIleGonderim = 1,
            AdresimdenAlinmali = 2,
            Diger = 3
        }

        public enum PaymentMethods
        {
            BankaHavalesiIlePesin = 1,
            BankaHavalesiIleTeslimattanSonra = 2,
            EldenOdeme = 3,
            KrediKartiIle = 4,
            PayPal = 5
        }

        public enum ContentArrayType
        {
            Yardim = 1,
            KullanimKosullari = 2,
            GuvenliOdeme = 3,
            Mail = 4
        }

        public enum Texts
        {
            Ara = 1,
            GirişYap = 2,
            UyeOl = 3,
            AnasayfaTitle = 4,
            AnasayfaAciklama = 5,
            AnasayfaSEOKeywords = 6,
            HakkimizdaYazisi = 47,
            VideolarYazisi = 48,
            BankaHavalesiText = 53,
            IletisimUstMetin = 170,
            IletisimAltMetin = 171,
            GuvenliOdemeBilgilerimAciklama = 172,
            Ucretler = 227,
            KullaniciEklemePop = 360,
            KullaniciEkleAltBildiri = 361,
            AnaSayfaAltYazi = 362,
            AnaSayfaAltYaziBaslik = 363,
            SaticiyaUrunSatiltiTitle = 367,
            AliciyaUrunSatinAldinizTitle = 375,
            AliciOnaylamamaNedeni = 377,
            MailAktivasyon = 378,
            AktivasyonOnay = 379,
            SaticiyaUrunSatildi = 380,
            AliciyaUrunSatinAldiniz = 381,
            UruneOnayVermenizGereklidir = 382,
            AliciOnay = 383,
            AliciRed = 384,
            IlanOnay = 385,
            IlanRed = 386,
            OdemeAktarildiMail = 402,
            SiparisIptalEdildi = 410,
            XMLListIcerik = 412,
            XMLSaatUyarisi = 413,
            XMLInsertIcerik = 414,
            XMLListBaslik = 415,
            XMLInsertBaslik = 416,
            Ayrac = 421,
            MesafeliSatisSozlesmesi = 432,
            HeraderIlanVer=448
        }


        public enum TextType
        {
            Normal = 1,
            ZenginIcerik = 2
        }

        public enum Languages
        {
            tr = 1,
            en = 2
        }

        public enum MoneyType
        {
            tl = 1,
            usd = 2,
            euro = 3,
            gbp = 4
        }

        public enum Dopings
        {
            AnaSayfadaGoruntuleme_7 = 1,
            AnaSayfadaGoruntuleme_30 = 2,
            KategoriSayfadaGoruntuleme_7 = 3,
            KategoriSayfadaGoruntuleme_30 = 4,
            Yeni_7 = 5,
            Yeni_30 = 6,
            FiyatiDustu_7 = 7,
            FiyatiDustu_30 = 8,
            Acil_7 = 9,
            Acil_30 = 10,
            SariCerceve_7 = 11,
            SariCerceve_30 = 12,
            Blog_7 = 13,
            Blog_30 = 14,
            AnasayfaBanner_7 = 15,
            AnasayfaBanner_30 = 16,
            LogoBanner_7 = 17,
            LogoBanner_30 = 18,
        }


        public enum Routing
        {
            Anasayfa = 1,
            IlanEkle = 2,
            UyeOl = 3,
            GirisYap = 4,
            Hakkimizda = 5,
            Videolar = 6,
            Yardim = 7,
            KullanimKosullari = 8,
            GuvenliOdemeGittiBu = 9,
            Ucretler = 10,
            Iletisim = 11,
            UyelikBilgilerim = 16,
            AdresBilgierim = 17,
            GuvenliOdemeBilgilerim = 18,
            ProfilSayfam = 19,
            SifreDegistir = 20,
            CariHesabim = 21,
            YaptigimYorumlar = 22,
            BegendigimIlanlar = 23,
            BannerEkle = 24,
            Bannerlarim = 25,
            CikisYap = 30,
            Ilanlarim = 31,
            Sepet = 32,
            SifremiUnuttum = 33,
            SifremiSifirla = 34,
            UyelikSozlesmesi = 35,
            HesabimiAktiflestir = 36,
            IlanVermeKurallari = 37,
            Ilan = 38,
            SaticiDigerIlanlari = 39,
            AdresEkle = 40,
            AdresDuzenle = 41,
            BannerDuzenle = 42,
            IlanDuzenle = 43,
            KimlikFotograflarim = 44,
            Alislarim = 45,
            Satislarim = 46,
            XMLYukle = 47,
            XMLListesi = 48
        }


        public enum CompanyType
        {
            SahisFirmas = 1,
            Anonim = 2,
        }

        public enum BannerType
        {
            Logo = 1,
            Banner = 2,
            Slider = 3
        }

        public enum PaymentType
        {
            LogoBanner,
            SliderBanner,
            IlanDoping,
            Ilan,
            BlogSureUzatma,
            BannerSureUzatma,
            BlogGirisi,
        }

        public enum SystemSettingName
        {
            GittiBuKomisyonuYuzde,
            IyzicoKomisyonuYuzde,
            IyzicoKomisyonuTL,
            OtomatikOdemeOnayi,
            YoneticiMailleri,
            AutoTransferDays,
            IletisimFormuMail,
            MinimumOdemeTutari,
            XMLMinimumPrice,
            xmlstart,
            xmlstop,
            MailServerSMTPPassword,
            MailServerSettings
        }

        public enum HomePageItemType
        {
            Ilan,
            Blog
        }

        public enum LogType
        {
            Payment
        }

        public enum MemberType
        {
            Buyer = 0,
            Seller
        }

        public enum OauthProviders
        {
            Facebook = 0
        }
        public enum ApprovalStatusEnum
        {
            Undefined = 0,
            WaitingforApproval = 1,
            Approved = 2,
            Rejected = 3
        }
    }
}