using NUnit.Framework;

public sealed class LevelPlayManagerTests
{
    [Test]
    public void LoadLevel_ReturnsValidationError_WhenDefinitionOrSettingsAreInvalid()
    {
        LevelPlayManager manager = CreateManager();
        LevelDefinition invalidDefinition = new LevelDefinition(1,
                                                                0,
                                                                1,
                                                                new ProcessDefinition[0],
                                                                new ResourceDefinition[0],
                                                                new BoardRuleDefinition[0]);

        LevelPlayCommandResult invalidDefinitionResult = manager.LoadLevel(invalidDefinition,
                                                                            new LevelPlaySettings(1, 2, 3));
        LevelPlayCommandResult invalidSettingsResult = manager.LoadLevel(CreateSingleSlotDefinition(),
                                                                          new LevelPlaySettings(3, 2, 1));

        Assert.That(invalidDefinitionResult.Success, Is.False);
        Assert.That(invalidDefinitionResult.Error, Is.EqualTo(ELevelPlayCommandError.InvalidDefinition));
        Assert.That(invalidDefinitionResult.ValidationMessageArray.Length, Is.GreaterThan(0));
        Assert.That(invalidSettingsResult.Success, Is.False);
        Assert.That(invalidSettingsResult.Error, Is.EqualTo(ELevelPlayCommandError.InvalidSettings));
    }

    [Test]
    public void RemoveConnection_NormalizesRemainingSelectionOrder()
    {
        LevelPlayManager manager = CreateManager();
        manager.LoadLevel(CreateTwoSlotDefinition(), new LevelPlaySettings(2, 3, 4));

        LevelPlayCommandResult firstAssignment = manager.AssignConnection(0, 1, 2);
        LevelPlayCommandResult secondAssignment = manager.AssignConnection(0, 0, 1);
        LevelPlayCommandResult removeResult = manager.RemoveConnection(firstAssignment.ConnectionId);

        Assert.That(firstAssignment.Success, Is.True);
        Assert.That(secondAssignment.Success, Is.True);
        Assert.That(removeResult.Success, Is.True);
        Assert.That(GetSlot(manager.CurrentLevel, 0, 0).SelectionOrder, Is.EqualTo(0));
        Assert.That(GetSlot(manager.CurrentLevel, 0, 1).ConnectionId, Is.EqualTo(-1));
    }

    [Test]
    public void ReplaceConnection_RestoresOriginalPlan_WhenNewReservationFails()
    {
        LevelPlayManager manager = CreateManager();
        manager.LoadLevel(CreateTwoSlotDefinition(), new LevelPlaySettings(2, 3, 4));
        LevelPlayCommandResult assignment = manager.AssignConnection(0, 0, 1);

        LevelPlayCommandResult replaceResult = manager.ReplaceConnection(0, 0, 3);
        LevelPlaySlotDTO slot = GetSlot(manager.CurrentLevel, 0, 0);
        LevelPlayConnectionDTO connection = manager.CurrentLevel.ConnectionArray[0];

        Assert.That(assignment.Success, Is.True);
        Assert.That(replaceResult.Success, Is.False);
        Assert.That(replaceResult.Error, Is.EqualTo(ELevelPlayCommandError.AssignmentRejected));
        Assert.That(connection.ResourceId, Is.EqualTo(1));
        Assert.That(slot.SelectionOrder, Is.EqualTo(0));
    }

    [Test]
    public void StartSimulation_ReturnsIncompletePlan_WhenAnySlotIsUnassigned()
    {
        LevelPlayManager manager = CreateManager();
        manager.LoadLevel(CreateTwoSlotDefinition(), new LevelPlaySettings(2, 3, 4));
        manager.AssignConnection(0, 0, 1);

        LevelPlayCommandResult result = manager.StartSimulation();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(ELevelPlayCommandError.IncompletePlan));
    }

    [Test]
    public void StartSimulation_CachesFullRoundResultAndAwardsStars()
    {
        LevelPlayManager manager = CreateManager();
        manager.LoadLevel(CreateSingleSlotDefinition(), new LevelPlaySettings(1, 2, 3));
        manager.AssignConnection(0, 0, 1);

        LevelPlayCommandResult result = manager.StartSimulation();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Simulation.EndState, Is.EqualTo(ELevelPlaySimulationEndState.Succeeded));
        Assert.That(result.Simulation.ClearRoundCount, Is.EqualTo(1));
        Assert.That(result.Simulation.StarCount, Is.EqualTo(3));
        Assert.That(result.Simulation.RoundArray.Length, Is.EqualTo(1));
        Assert.That(manager.CurrentLevel.Phase, Is.EqualTo(ELevelPlayPhase.ResultReady));
    }

    [Test]
    public void RestartLevel_RebuildsBoardAndPreservesLastPlan()
    {
        LevelPlayManager manager = CreateManager();
        manager.LoadLevel(CreateTwoSlotDefinition(), new LevelPlaySettings(2, 3, 4));
        manager.AssignConnection(0, 1, 2);
        manager.AssignConnection(0, 0, 1);
        manager.StartSimulation();

        LevelPlayCommandResult restartResult = manager.RestartLevel();

        Assert.That(restartResult.Success, Is.True);
        Assert.That(manager.CurrentLevel.Phase, Is.EqualTo(ELevelPlayPhase.Planning));
        Assert.That(manager.CurrentSimulation, Is.Null);
        Assert.That(manager.CurrentLevel.ConnectionArray.Length, Is.EqualTo(2));
        Assert.That(GetSlot(manager.CurrentLevel, 0, 1).SelectionOrder, Is.EqualTo(0));
        Assert.That(GetSlot(manager.CurrentLevel, 0, 0).SelectionOrder, Is.EqualTo(1));
    }

    [Test]
    public void FocusResource_MapsRelayFocusToDTO()
    {
        LevelPlayManager manager = CreateManager();
        manager.LoadLevel(CreateRelayDefinition(), new LevelPlaySettings(1, 2, 3));

        ResourceFocusDTO focus = manager.FocusResource(1);

        Assert.That(focus.Success, Is.True);
        Assert.That(focus.FocusKind, Is.EqualTo(ELevelPlayFocusKind.Pair));
        Assert.That(focus.HighlightedResourceIdArray, Does.Contain(1));
        Assert.That(focus.HighlightedResourceIdArray, Does.Contain(2));
        Assert.That(focus.ActiveBoardRuleIdArray, Does.Contain(9));
    }

    private static LevelPlayManager CreateManager()
    {
        return new LevelPlayManager(new LevelBoardFactory(), new LevelDefinitionValidator());
    }

    private static LevelDefinition CreateSingleSlotDefinition()
    {
        ProcessDefinition process = new ProcessDefinition(0,
                                                           new BoardPosition(0, 0),
                                                           new[]
                                                           {
                                                               new ProcessSlotDefinition(0, new ColorId(1), 0),
                                                           });
        ResourceDefinition resource = new ResourceDefinition(1,
                                                             new BoardPosition(0, 1),
                                                             new ColorId(1),
                                                             1,
                                                             new ResourceRuleDefinition[]
                                                             {
                                                                 new NoResourceRuleDefinition(),
                                                             });

        return new LevelDefinition(1,
                                   1,
                                   2,
                                   new[] { process },
                                   new[] { resource },
                                   new BoardRuleDefinition[0]);
    }

    private static LevelDefinition CreateTwoSlotDefinition()
    {
        ProcessDefinition process = new ProcessDefinition(0,
                                                           new BoardPosition(0, 0),
                                                           new[]
                                                           {
                                                               new ProcessSlotDefinition(0, new ColorId(1), 0),
                                                               new ProcessSlotDefinition(1, new ColorId(2), 1),
                                                           });
        ResourceDefinition firstResource = new ResourceDefinition(1,
                                                                  new BoardPosition(0, 1),
                                                                  new ColorId(1),
                                                                  1,
                                                                  new ResourceRuleDefinition[]
                                                                  {
                                                                      new NoResourceRuleDefinition(),
                                                                  });
        ResourceDefinition secondResource = new ResourceDefinition(2,
                                                                   new BoardPosition(1, 1),
                                                                   new ColorId(2),
                                                                   1,
                                                                   new ResourceRuleDefinition[]
                                                                   {
                                                                       new NoResourceRuleDefinition(),
                                                                   });
        ResourceDefinition rejectedResource = new ResourceDefinition(3,
                                                                    new BoardPosition(2, 1),
                                                                    new ColorId(3),
                                                                    1,
                                                                    new ResourceRuleDefinition[]
                                                                    {
                                                                        new NoResourceRuleDefinition(),
                                                                    });

        return new LevelDefinition(2,
                                   3,
                                   2,
                                   new[] { process },
                                   new[] { firstResource, secondResource, rejectedResource },
                                   new BoardRuleDefinition[0]);
    }

    private static LevelDefinition CreateRelayDefinition()
    {
        ProcessDefinition process = new ProcessDefinition(0,
                                                           new BoardPosition(0, 0),
                                                           new[]
                                                           {
                                                               new ProcessSlotDefinition(0, new ColorId(1), 0),
                                                           });
        ResourceDefinition firstResource = new ResourceDefinition(1,
                                                                  new BoardPosition(0, 1),
                                                                  new ColorId(1),
                                                                  1,
                                                                  new ResourceRuleDefinition[]
                                                                  {
                                                                      new NoResourceRuleDefinition(),
                                                                  });
        ResourceDefinition secondResource = new ResourceDefinition(2,
                                                                   new BoardPosition(1, 1),
                                                                   new ColorId(1),
                                                                   1,
                                                                   new ResourceRuleDefinition[]
                                                                   {
                                                                       new NoResourceRuleDefinition(),
                                                                   });
        RelayRuleDefinition relay = new RelayRuleDefinition(9, ERelayType.Link, 1, 2, 0);

        return new LevelDefinition(3,
                                   2,
                                   2,
                                   new[] { process },
                                   new[] { firstResource, secondResource },
                                   new BoardRuleDefinition[] { relay });
    }

    private static LevelPlaySlotDTO GetSlot(LevelPlayDTO level, int processId, int slotId)
    {
        for (int i = 0; i < level.ProcessArray.Length; i++)
        {
            LevelPlayProcessDTO process = level.ProcessArray[i];

            if (process.Id != processId)
            {
                continue;
            }

            for (int slotIndex = 0; slotIndex < process.SlotArray.Length; slotIndex++)
            {
                LevelPlaySlotDTO slot = process.SlotArray[slotIndex];

                if (slot.Id == slotId)
                {
                    return slot;
                }
            }
        }

        Assert.Fail($"Slot not found: P{processId}:S{slotId}");
        return null;
    }
}
