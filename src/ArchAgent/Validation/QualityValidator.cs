using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArchAgent.Model;

namespace ArchAgent.Validation;

public static class QualityValidator
{
    public static QualityReport Validate(string outputRoot, IEnumerable<string> requiredFiles, int assumptionCount)
    {
        var warnings = new List<string>();
        int total = 0;
        int present = 0;

        foreach (var file in requiredFiles)
        {
            total++;
            if (File.Exists(file))
            {
                var length = new FileInfo(file).Length;
                if (length > 0)
                {
                    present++;
                }
                else
                {
                    warnings.Add($"Empty artifact: {file}");
                }
            }
            else
            {
                warnings.Add($"Missing artifact: {file}");
            }
        }

        var completeness = total == 0 ? 0 : (int)Math.Round(100.0 * present / total);
        var consistency = warnings.Count == 0 ? 100 : Math.Max(60, 100 - warnings.Count * 5);

        return new QualityReport
        {
            CompletenessScore = completeness,
            ConsistencyScore = consistency,
            AssumptionCount = assumptionCount,
            Warnings = warnings
        };
    }
}
