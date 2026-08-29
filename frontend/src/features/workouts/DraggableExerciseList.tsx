import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import {
  Animated,
  PanResponder,
  Pressable,
  StyleSheet,
  View,
  type AccessibilityActionEvent,
} from "react-native";

import { AppText } from "../../components/AppText";
import { colors, layout, spacing } from "../../theme/tokens";
import {
  formatPrescription,
  type UnitSystem,
  type WorkoutExerciseDraft,
} from "./workout-draft";

type DraggableExerciseListProps = {
  drafts: readonly WorkoutExerciseDraft[];
  errors: Record<string, string>;
  onAutoScroll: (absoluteY: number) => void;
  onEdit: (exerciseId: string) => void;
  onReorder: (fromIndex: number, toIndex: number) => void;
  unitSystem: UnitSystem;
};

type ItemLayout = { height: number; y: number };

export function DraggableExerciseList({
  drafts,
  errors,
  onAutoScroll,
  onEdit,
  onReorder,
  unitSystem,
}: DraggableExerciseListProps) {
  const [layouts, setLayouts] = useState(() => new Map<string, ItemLayout>());
  const [dragTop] = useState(() => new Animated.Value(0));
  const dragStartTop = useRef(0);
  const [activeId, setActiveId] = useState<string>();
  const [targetIndex, setTargetIndex] = useState<number>();
  const activeIdRef = useRef<string | undefined>(undefined);
  const activeIndexRef = useRef(-1);
  const activeLayoutRef = useRef<ItemLayout | undefined>(undefined);
  const targetIndexRef = useRef<number | undefined>(undefined);
  const draftsRef = useRef(drafts);
  const layoutsRef = useRef(layouts);
  const onAutoScrollRef = useRef(onAutoScroll);
  const onReorderRef = useRef(onReorder);

  useEffect(() => {
    draftsRef.current = drafts;
    onAutoScrollRef.current = onAutoScroll;
    onReorderRef.current = onReorder;
  }, [drafts, onAutoScroll, onReorder]);

  const activeIndex = activeId
    ? drafts.findIndex((draft) => draft.exercise.id === activeId)
    : -1;
  const activeDraft = activeIndex >= 0 ? drafts[activeIndex] : undefined;
  const activeLayout = activeId ? layouts.get(activeId) : undefined;

  const startDrag = useCallback(
    (exerciseId: string) => {
      const layout = layoutsRef.current.get(exerciseId);
      const index = draftsRef.current.findIndex(
        (draft) => draft.exercise.id === exerciseId,
      );
      if (!layout || index < 0) return;

      dragStartTop.current = layout.y;
      dragTop.setValue(layout.y);
      activeIdRef.current = exerciseId;
      activeIndexRef.current = index;
      activeLayoutRef.current = layout;
      targetIndexRef.current = index;
      setActiveId(exerciseId);
      setTargetIndex(index);
    },
    [dragTop],
  );

  const moveDrag = useCallback(
    (translationY: number, absoluteY: number) => {
      const layout = activeLayoutRef.current;
      if (!activeIdRef.current || !layout) return;

      const nextTop = dragStartTop.current + translationY;
      dragTop.setValue(nextTop);
      const center = nextTop + layout.height / 2;
      const nextTarget = findTargetIndex(
        draftsRef.current,
        layoutsRef.current,
        center,
        activeIndexRef.current,
      );
      targetIndexRef.current = nextTarget;
      setTargetIndex((current) =>
        current === nextTarget ? current : nextTarget,
      );
      onAutoScrollRef.current(absoluteY);
    },
    [dragTop],
  );

  const finishDrag = useCallback((succeeded: boolean) => {
    const fromIndex = activeIndexRef.current;
    const toIndex = targetIndexRef.current;

    activeIdRef.current = undefined;
    activeIndexRef.current = -1;
    activeLayoutRef.current = undefined;
    targetIndexRef.current = undefined;
    setActiveId(undefined);
    setTargetIndex(undefined);

    if (
      succeeded &&
      fromIndex >= 0 &&
      toIndex !== undefined &&
      fromIndex !== toIndex
    ) {
      onReorderRef.current(fromIndex, toIndex);
    }
  }, []);

  const insertionTop = calculateInsertionTop(
    drafts,
    layouts,
    activeIndex,
    targetIndex,
  );

  return (
    <View style={styles.list}>
      {drafts.map((draft, index) => (
        <View
          key={draft.exercise.id}
          testID={`exercise-layout-${draft.exercise.id}`}
          onLayout={(event) => {
            const { height, y } = event.nativeEvent.layout;
            setLayouts((current) => {
              const existing = current.get(draft.exercise.id);
              if (existing?.height === height && existing.y === y)
                return current;
              const next = new Map(current);
              next.set(draft.exercise.id, { height, y });
              layoutsRef.current = next;
              return next;
            });
          }}
          style={activeId === draft.exercise.id && styles.activePlaceholder}
        >
          <DraggableExerciseRow
            draft={draft}
            error={errors[draft.exercise.id]}
            index={index}
            itemCount={drafts.length}
            onDragEnd={finishDrag}
            onDragMove={moveDrag}
            onDragStart={startDrag}
            onEdit={() => onEdit(draft.exercise.id)}
            onMove={(toIndex) => onReorder(index, toIndex)}
            unitSystem={unitSystem}
          />
        </View>
      ))}

      {activeDraft && activeLayout ? (
        <Animated.View
          pointerEvents="none"
          style={[
            styles.dragOverlay,
            { minHeight: activeLayout.height, top: dragTop },
          ]}
        >
          <ExerciseRowContent
            draft={activeDraft}
            index={activeIndex}
            unitSystem={unitSystem}
          />
        </Animated.View>
      ) : null}

      {insertionTop !== null ? (
        <View
          pointerEvents="none"
          style={[styles.insertion, { top: insertionTop }]}
        />
      ) : null}
    </View>
  );
}

function DraggableExerciseRow({
  draft,
  error,
  index,
  itemCount,
  onDragEnd,
  onDragMove,
  onDragStart,
  onEdit,
  onMove,
  unitSystem,
}: {
  draft: WorkoutExerciseDraft;
  error?: string;
  index: number;
  itemCount: number;
  onDragEnd: (succeeded: boolean) => void;
  onDragMove: (translationY: number, absoluteY: number) => void;
  onDragStart: (exerciseId: string) => void;
  onEdit: () => void;
  onMove: (toIndex: number) => void;
  unitSystem: UnitSystem;
}) {
  const panResponder = useMemo(
    () =>
      PanResponder.create({
        onStartShouldSetPanResponderCapture: () => true,
        onMoveShouldSetPanResponderCapture: () => true,
        onPanResponderGrant: () => onDragStart(draft.exercise.id),
        onPanResponderMove: (_event, gestureState) => {
          onDragMove(gestureState.dy, gestureState.moveY);
        },
        onPanResponderRelease: () => onDragEnd(true),
        onPanResponderTerminate: () => onDragEnd(false),
        onPanResponderTerminationRequest: () => false,
        onShouldBlockNativeResponder: () => true,
      }),
    [draft.exercise.id, onDragEnd, onDragMove, onDragStart],
  );

  const accessibilityActions = [
    ...(index > 0 ? [{ name: "decrement" as const, label: "Move up" }] : []),
    ...(index < itemCount - 1
      ? [{ name: "increment" as const, label: "Move down" }]
      : []),
  ];

  const handleAccessibilityAction = (event: AccessibilityActionEvent) => {
    if (event.nativeEvent.actionName === "decrement" && index > 0) {
      onMove(index - 1);
    } else if (
      event.nativeEvent.actionName === "increment" &&
      index < itemCount - 1
    ) {
      onMove(index + 1);
    }
  };

  return (
    <View>
      <View style={styles.row}>
        <View
          {...panResponder.panHandlers}
          accessible
          accessibilityActions={accessibilityActions}
          accessibilityHint="Drag to reorder"
          accessibilityLabel={`Reorder ${draft.exercise.name}`}
          accessibilityRole="adjustable"
          accessibilityValue={{
            min: 1,
            max: itemCount,
            now: index + 1,
            text: `Position ${index + 1} of ${itemCount}`,
          }}
          onAccessibilityAction={handleAccessibilityAction}
          style={styles.dragHandle}
          testID={`reorder-${draft.exercise.id}`}
        >
          <AppText style={styles.dragGlyph} tone="secondary">
            ≡
          </AppText>
        </View>

        <Pressable
          accessibilityHint="Edit planned targets"
          accessibilityRole="button"
          onPress={onEdit}
          style={({ pressed }) => [styles.rowButton, pressed && styles.pressed]}
        >
          <ExerciseRowContent
            draft={draft}
            index={index}
            unitSystem={unitSystem}
          />
        </Pressable>
      </View>
      {error ? (
        <AppText accessibilityLiveRegion="polite" style={styles.error}>
          {error}
        </AppText>
      ) : null}
    </View>
  );
}

function ExerciseRowContent({
  draft,
  index,
  unitSystem,
}: {
  draft: WorkoutExerciseDraft;
  index: number;
  unitSystem: UnitSystem;
}) {
  return (
    <View style={styles.rowContent}>
      <View style={styles.position}>
        <AppText style={styles.positionLabel} variant="label">
          {index + 1}
        </AppText>
      </View>
      <View style={styles.exerciseCopy}>
        <AppText variant="label">{draft.exercise.name}</AppText>
        <AppText style={styles.prescription} tone="secondary">
          {formatPrescription(draft, unitSystem)}
        </AppText>
      </View>
      <AppText tone="accent" variant="label">
        Edit
      </AppText>
    </View>
  );
}

function findTargetIndex(
  drafts: readonly WorkoutExerciseDraft[],
  layouts: Map<string, ItemLayout>,
  center: number,
  activeIndex: number,
) {
  if (activeIndex < 0 || activeIndex >= drafts.length) return activeIndex;

  let targetIndex = activeIndex;
  for (let index = activeIndex + 1; index < drafts.length; index += 1) {
    const layout = layouts.get(drafts[index].exercise.id);
    if (layout && center >= layout.y + layout.height / 2) {
      targetIndex = index;
    }
  }
  for (let index = activeIndex - 1; index >= 0; index -= 1) {
    const layout = layouts.get(drafts[index].exercise.id);
    if (layout && center <= layout.y + layout.height / 2) {
      targetIndex = index;
    }
  }
  return targetIndex;
}

function calculateInsertionTop(
  drafts: readonly WorkoutExerciseDraft[],
  layouts: Map<string, ItemLayout>,
  activeIndex: number,
  targetIndex?: number,
) {
  if (
    activeIndex < 0 ||
    targetIndex === undefined ||
    activeIndex === targetIndex
  ) {
    return null;
  }

  const target = layouts.get(drafts[targetIndex]?.exercise.id ?? "");
  if (!target) return null;
  return targetIndex > activeIndex ? target.y + target.height : target.y;
}

const styles = StyleSheet.create({
  list: {
    position: "relative",
    borderTopWidth: StyleSheet.hairlineWidth,
    borderTopColor: colors.border,
  },
  row: {
    minHeight: 76,
    flexDirection: "row",
    alignItems: "stretch",
    borderBottomWidth: StyleSheet.hairlineWidth,
    borderBottomColor: colors.border,
    backgroundColor: colors.canvas,
  },
  dragHandle: {
    width: layout.minimumTouchTarget,
    minHeight: 76,
    alignItems: "center",
    justifyContent: "center",
  },
  dragGlyph: {
    fontSize: 23,
    letterSpacing: -4,
  },
  rowButton: {
    flex: 1,
    justifyContent: "center",
    paddingVertical: spacing.md,
    paddingRight: spacing.sm,
  },
  rowContent: {
    flex: 1,
    flexDirection: "row",
    alignItems: "center",
    gap: spacing.md,
  },
  position: {
    width: 30,
    height: 30,
    alignItems: "center",
    justifyContent: "center",
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 8,
  },
  positionLabel: {
    fontSize: 13,
  },
  exerciseCopy: {
    flex: 1,
    gap: spacing.xs,
  },
  prescription: {
    fontSize: 14,
  },
  pressed: {
    opacity: 0.68,
  },
  activePlaceholder: {
    opacity: 0.2,
  },
  dragOverlay: {
    position: "absolute",
    right: 0,
    left: layout.minimumTouchTarget,
    zIndex: 2,
    justifyContent: "center",
    borderWidth: 1,
    borderColor: colors.focus,
    paddingHorizontal: spacing.sm,
    backgroundColor: colors.surfaceRaised,
    shadowColor: "#000000",
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.3,
    shadowRadius: 12,
  },
  insertion: {
    position: "absolute",
    right: 0,
    left: 0,
    zIndex: 3,
    height: 3,
    borderRadius: 2,
    backgroundColor: colors.accentHighlight,
  },
  error: {
    paddingTop: spacing.sm,
    paddingHorizontal: spacing.md,
    color: colors.statusDanger,
    fontSize: 14,
  },
});
