using Unity.Collections;

public class G
{
    public readonly static int numberOfPoints = 1000;
    public readonly static int numberOfStages = 200;
    public readonly static int numberOfSites = 100;
    public readonly static int maxNPCPopulation = 1000;
    public readonly static int numberOfPlays = 100;
    public readonly static int numberOfTasks = 100;
    public readonly static int maxValidPlays = numberOfPlays / 10;
    public readonly static int maxValidTasks = numberOfTasks / 10;
    public readonly static int maxLazyCharacters = maxNPCPopulation / 10;
    public readonly static int maxPlayerPopulation = 1000;
    public readonly static int maxTotalPopulation = maxNPCPopulation + maxPlayerPopulation;
    public readonly static int numberOfQuests = 100;
    public readonly static int maxCurrentQuests = 10000;
    public readonly static int maxQuestsPerPlayer = maxCurrentQuests / maxPlayerPopulation;
    public readonly static int rareFactionEvents = 10;
    public readonly static int maxFactions = maxTotalPopulation * 2;
    public readonly static int numberOfDeeds = 10;
    public readonly static int maxRelationships = maxTotalPopulation * 10;
    public readonly static int memoriesPerCharacter = 50;
    public readonly static int maxMemories = maxTotalPopulation * memoriesPerCharacter;
    public readonly static int maxPerQuestSubjectsObjects = 10;
    public readonly static int maxQuestSubjects = maxCurrentQuests * maxPerQuestSubjectsObjects;
    public readonly static int maxQuestObjects = maxCurrentQuests * maxPerQuestSubjectsObjects;
    public readonly static int occupantsPerPoint = 10;
    public readonly static int pointsPerStage = 10;
    public readonly static int occupantsPerStage = 100;
    public readonly static int occupantsPerSite = 1000;
    public readonly static int stagesPerSite = 10;
    public readonly static int valuesPerDeed = 10;
    public readonly static int valuesTraits = 1;
    public readonly static int maxFactionParents = 10;
    public readonly static int totalMoodFloat = maxNPCPopulation * 3;
    public readonly static int maxPerRumorWitnesses = 10;
    public readonly static int maxTotalListeners = maxTotalPopulation * maxPerRumorWitnesses;
    public readonly static int relationshipValues = 10;
    public readonly static int maxRelationshipValues = maxTotalPopulation * relationshipValues;
    public readonly static float arrousalImportance = .2f;
    public readonly static float traitAlignmentImportance = .2f;
}
