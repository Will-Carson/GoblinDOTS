using Unity.Collections;

public class G
{
    public static int numberOfPoints = 1000;
    public static int numberOfStages = 200;
    public static int numberOfSites = 100;
    public static int maxNPCPopulation = 1000;
    public static int numberOfPlays = 100;
    public static int numberOfTasks = 100;
    public static int maxValidPlays = numberOfPlays / 10;
    public static int maxValidTasks = numberOfTasks / 10;
    public static int maxLazyCharacters = maxNPCPopulation / 10;
    public static int maxPlayerPopulation = 1000;
    public static int maxTotalPopulation = maxNPCPopulation + maxPlayerPopulation;
    public static int numberOfQuests = 100;
    public static int maxCurrentQuests = 10000;
    public static int maxQuestsPerPlayer = maxCurrentQuests / maxPlayerPopulation;
    public static int rareFactionEvents = 10;
    public static int maxFactions = maxTotalPopulation * 2;
    public static int numberOfDeeds = 10;
    public static int maxRelationships = maxTotalPopulation * 10;
    public static int maxMemories = maxTotalPopulation * 50;
    public static int maxPerQuestSubjectsObjects = 10;
    public static int maxQuestSubjects = maxCurrentQuests * maxPerQuestSubjectsObjects;
    public static int maxQuestObjects = maxCurrentQuests * maxPerQuestSubjectsObjects;
    public static int occupantsPerPoint = 10;
    public static int pointsPerStage = 10;
    public static int occupantsPerStage = 100;
    public static int occupantsPerSite = 1000;
    public static int stagesPerSite = 10;
    public static int valuesPerDeed = 10;
    public static int personalityTraits = 10;
    public static int maxFactionParents = 10;
    public static int totalMoodFloat = maxNPCPopulation * 3;
}
