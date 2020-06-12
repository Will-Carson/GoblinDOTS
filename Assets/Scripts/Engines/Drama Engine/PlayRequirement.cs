public struct PlayRequirement
{
    public int playId;
    public int subjectX, subjectY, subjectZ;
    public ValueRequirement cXValues;
    public ValueRequirement cYValues;
    public ValueRequirement cZValues;
    public FullRelationship relationshipX;
    public FullRelationship relationshipY;
    public TemplateMemory templateMemory;

    public static bool TryAddFullRelationship(int id, FullRelationship tryAdd, FullRelationship template, PlayRequirement pr, out PlayRequirement newPlayRequirement)
    {
        int tempSubjectX = 0, tempSubjectY = 0, tempSubjectZ = 0;
        bool test1 = false, test2 = false;
        newPlayRequirement = pr;

        if (template.subjectX == 1) if (pr.subjectX == 0 || pr.subjectX == tryAdd.subjectX) { tempSubjectX = tryAdd.subjectX; test1 = true; }
        if (template.subjectX == 2) if (pr.subjectY == 0 || pr.subjectY == tryAdd.subjectX) { tempSubjectY = tryAdd.subjectX; test1 = true; }
        if (template.subjectX == 3) if (pr.subjectZ == 0 || pr.subjectZ == tryAdd.subjectX) { tempSubjectZ = tryAdd.subjectX; test1 = true; }

        if (template.subjectY == 1) if (pr.subjectX == 0 || pr.subjectX == tryAdd.subjectY) { tempSubjectX = tryAdd.subjectY; test2 = true; }
        if (template.subjectY == 2) if (pr.subjectY == 0 || pr.subjectY == tryAdd.subjectY) { tempSubjectY = tryAdd.subjectY; test2 = true; }
        if (template.subjectY == 3) if (pr.subjectZ == 0 || pr.subjectZ == tryAdd.subjectY) { tempSubjectZ = tryAdd.subjectY; test2 = true; }

        if (test1 && test2)
        {
            newPlayRequirement.subjectX = tempSubjectX;
            newPlayRequirement.subjectY = tempSubjectY;
            newPlayRequirement.subjectZ = tempSubjectZ;

            if (id == 1) newPlayRequirement.relationshipX = tryAdd;
            if (id == 2) newPlayRequirement.relationshipY = tryAdd;

            return true;
        }
        return false;
    }

    public static bool CheckValuesInRange(DataValues values, ValueRequirement template)
    {
        if (DataValues.InRange(template, values)) return true;
        return false;
    }

    public static bool CheckValidMemory(TemplateMemory memory, TemplateMemory template, PlayRequirement pr, out PlayRequirement newPlayRequirement)
    {
        newPlayRequirement = pr;
        if (template.deed != memory.deed) return false;

        int tempSubjectX = 0, tempSubjectY = 0;
        bool test1 = false, test2 = false;

        if (template.subjectX == 1) if (memory.subjectX == pr.subjectX) { tempSubjectX = pr.subjectX; test1 = true; }
        if (template.subjectX == 2) if (memory.subjectX == pr.subjectY) { tempSubjectX = pr.subjectY; test1 = true; }
        if (template.subjectX == 3) if (memory.subjectX == pr.subjectZ) { tempSubjectX = pr.subjectZ; test1 = true; }

        if (template.subjectY == 1) if (memory.subjectY == pr.subjectX) { tempSubjectY = pr.subjectX; test2 = true; }
        if (template.subjectY == 2) if (memory.subjectY == pr.subjectY) { tempSubjectY = pr.subjectY; test2 = true; }
        if (template.subjectY == 3) if (memory.subjectY == pr.subjectZ) { tempSubjectY = pr.subjectZ; test2 = true; }
        
        if (test1 && test2)
        {
            newPlayRequirement.templateMemory.subjectX = tempSubjectX;
            newPlayRequirement.templateMemory.subjectY = tempSubjectY;
            newPlayRequirement.templateMemory.deed = memory.deed;
            return true;
        }

        return false;
    }
}