namespace DeveloperNetwork.Models
{
    public class GLOutput
    {
        public GLOutput()
        {
        }

        public GLOutput(GLOutput glOutput, GeocodeComponents geocode)
        {
            Login = glOutput.Login;
            GitLocation = glOutput.GitLocation;
            Locality = geocode.Locality;
            AdminAreaLevel1 = geocode.AdminAreaLevel1;
            AdminAreaLevel2 = geocode.AdminAreaLevel2;
            Country = geocode.Country;
        }

        public string Login { get; set; }

        public string GitLocation { get; set; }

        public string Locality { get; private set; }

        public string AdminAreaLevel1 { get; }

        public string AdminAreaLevel2 { get; }

        public string Country { get; }
    }
}
