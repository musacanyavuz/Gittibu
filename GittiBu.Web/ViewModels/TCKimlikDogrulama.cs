namespace GittiBu.Web.ViewModels
{
    public class TCKimlikDogrulama
    {
        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true,
            Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://schemas.xmlsoap.org/soap/envelope/",
            IsNullable = false)]
        public partial class Envelope
        {
            private EnvelopeBody bodyField;

            /// <remarks/>
            public EnvelopeBody Body
            {
                get { return this.bodyField; }
                set { this.bodyField = value; }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true,
            Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public partial class EnvelopeBody
        {
            private TCKimlikNoDogrula tCKimlikNoDogrulaField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://tckimlik.nvi.gov.tr/WS")]
            public TCKimlikNoDogrula TCKimlikNoDogrula
            {
                get { return this.tCKimlikNoDogrulaField; }
                set { this.tCKimlikNoDogrulaField = value; }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://tckimlik.nvi.gov.tr/WS")]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://tckimlik.nvi.gov.tr/WS", IsNullable = false)]
        public partial class TCKimlikNoDogrula
        {
            private ulong tCKimlikNoField;

            private string adField;

            private string soyadField;

            private ushort dogumYiliField;

            /// <remarks/>
            public ulong TCKimlikNo
            {
                get { return this.tCKimlikNoField; }
                set { this.tCKimlikNoField = value; }
            }

            /// <remarks/>
            public string Ad
            {
                get { return this.adField; }
                set { this.adField = value; }
            }

            /// <remarks/>
            public string Soyad
            {
                get { return this.soyadField; }
                set { this.soyadField = value; }
            }

            /// <remarks/>
            public ushort DogumYili
            {
                get { return this.dogumYiliField; }
                set { this.dogumYiliField = value; }
            }
        }
    }
}