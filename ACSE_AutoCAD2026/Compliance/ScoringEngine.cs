using System;

namespace ACSE.AutoCAD2026.Compliance
{
    public static class ScoringEngine
    {
        public static double CalculateScore(ScanResult result)
        {
            if (result.GetTotalEntities() == 0)
                return 100;

            // Use total violations count (includes all violation types)
            int totalViolations = result.TotalViolations;

            // Calculate percentage of compliant entities
            // Multiple violations on same entity still count against that entity
            double violationRatio = (double)totalViolations / result.GetTotalEntities();

            // Score can't go below 0
            double score = 100 - (violationRatio * 100);
            return Math.Max(0, score);
        }
    }
}