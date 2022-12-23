using System.Collections.Generic;

using Newtonsoft.Json;

using Telegram.Bot.Types;

namespace Hackathonbot
{
    public class Project
    {
        [JsonIgnore]
        public Message PrevMessage { get; set; }
        [JsonIgnore]
        public Message TeamСompositionMessage { get; set; }
        [JsonIgnore]
        public Message AlertMessage { get; set; }
        public string TeamName { get; set; }
        public string SchoolName { get; set; }
        public List<TeamMember> TeamMembers { get; set; } = new List<TeamMember>();
        public string ProjetName { get; set; }
        public string CaseNumber { get; set; }
        public Telegram.Bot.Types.File Presentation { get; set; }
    }

    public class TeamMember
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string MemberInfo { get; set; }
    }
}