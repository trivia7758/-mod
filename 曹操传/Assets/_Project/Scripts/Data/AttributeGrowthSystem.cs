using CaoCao.Common;

namespace CaoCao.Data
{
    /// <summary>
    /// Five-dimension attribute growth system (五维成长系统).
    ///
    /// Each attribute has an initial grade (档位) that determines:
    /// 1. The threshold table row (promotion breakpoints)
    /// 2. Current grade based on current attribute value
    /// 3. Growth per level = grade value (C=1, B=2, A=3, S=4, SS=5, EX=6, EX+=7)
    /// </summary>
    public static class AttributeGrowthSystem
    {
        // Threshold table: thresholds[initialGrade][targetGrade]
        // -1 means that grade is unreachable (hero starts above it)
        // Row = initial grade, Column = grade threshold
        //                          C    B    A    S    SS   EX   EX+
        static readonly int[][] Thresholds = new[]
        {
            new[] {   0,  70, 120, 170, 220, 270, 320 }, // Initial C
            new[] {  -1,  50,  90, 140, 190, 240, 290 }, // Initial B
            new[] {  -1,  -1,  70, 110, 160, 210, 260 }, // Initial A
            new[] {  -1,  -1,  -1,  85, 130, 180, 230 }, // Initial S
            new[] {  -1,  -1,  -1,  -1,  90, 150, 200 }, // Initial SS
            new[] {  -1,  -1,  -1,  -1,  -1, 120, 170 }, // Initial EX
            new[] {  -1,  -1,  -1,  -1,  -1,  -1, 140 }, // Initial EX+
        };

        /// <summary>
        /// Get the current grade based on attribute value and initial grade.
        /// </summary>
        public static GrowthGrade GetCurrentGrade(int attributeValue, GrowthGrade initialGrade)
        {
            int row = (int)initialGrade;
            var thresholds = Thresholds[row];

            // Walk from highest grade down to find current grade
            for (int g = 6; g >= 0; g--)
            {
                if (thresholds[g] >= 0 && attributeValue >= thresholds[g])
                    return (GrowthGrade)g;
            }

            // Fallback: return initial grade
            return initialGrade;
        }

        /// <summary>
        /// Get growth value per level for a given grade.
        /// C=1, B=2, A=3, S=4, SS=5, EX=6, EX+=7
        /// </summary>
        public static int GetGrowthValue(GrowthGrade grade)
        {
            return (int)grade + 1;
        }

        /// <summary>
        /// Calculate attribute growth for a single level-up.
        /// Returns the amount to add to the attribute.
        /// </summary>
        public static int CalculateLevelUpGrowth(int currentValue, GrowthGrade initialGrade)
        {
            var currentGrade = GetCurrentGrade(currentValue, initialGrade);
            return GetGrowthValue(currentGrade);
        }

        /// <summary>
        /// Calculate attribute value at a given level, starting from base value.
        /// Simulates leveling from 1 to targetLevel.
        /// </summary>
        public static int CalculateAttributeAtLevel(int baseValue, GrowthGrade initialGrade, int targetLevel)
        {
            int value = baseValue;
            for (int lvl = 1; lvl < targetLevel; lvl++)
            {
                value += CalculateLevelUpGrowth(value, initialGrade);
            }
            return value;
        }

        /// <summary>
        /// Get the threshold to reach a specific grade from a given initial grade.
        /// Returns -1 if unreachable.
        /// </summary>
        public static int GetThreshold(GrowthGrade initialGrade, GrowthGrade targetGrade)
        {
            return Thresholds[(int)initialGrade][(int)targetGrade];
        }

        /// <summary>
        /// Get the display name for a grade (Chinese).
        /// </summary>
        public static string GetGradeDisplayName(GrowthGrade grade)
        {
            return grade switch
            {
                GrowthGrade.C => "C",
                GrowthGrade.B => "B",
                GrowthGrade.A => "A",
                GrowthGrade.S => "S",
                GrowthGrade.SS => "SS",
                GrowthGrade.EX => "EX",
                GrowthGrade.EXPlus => "EX+",
                _ => "?"
            };
        }

        /// <summary>
        /// Get the color hex for a grade (for UI display).
        /// </summary>
        public static string GetGradeColorHex(GrowthGrade grade)
        {
            return grade switch
            {
                GrowthGrade.C      => "#AAAAAA", // grey
                GrowthGrade.B      => "#FFFFFF", // white
                GrowthGrade.A      => "#66CC66", // green
                GrowthGrade.S      => "#6699FF", // blue
                GrowthGrade.SS     => "#CC66FF", // purple
                GrowthGrade.EX     => "#FFCC00", // gold
                GrowthGrade.EXPlus => "#FF6633", // orange-red
                _ => "#FFFFFF"
            };
        }
    }
}
