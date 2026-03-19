using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Dtos.BulkProvisioning
{
    public class CsvParseResult
    {
        public bool Success { get; set; }
        public List<CsvInvitationRow> ValidRows { get; set; } = new();
        public List<CsvParseError> Errors { get; set; } = new();
        public int TotalRows { get; set; }
        public int ValidRowCount => ValidRows.Count;
        public int ErrorCount => Errors.Count;

        public bool HasErrors => Errors.Any();
    }
}
