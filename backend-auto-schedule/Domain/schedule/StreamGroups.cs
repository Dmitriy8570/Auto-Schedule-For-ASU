using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Domain.schedule
{
    public class StreamGroups
    {
        public Guid GroupId { get; set; }
        public Guid StreamId { get; set; }

        public Group Group { get; set; }
        public Stream Stream { get; set; }
    }
}
