import { useMemo, useRef, useState } from "react";
import {
  Animated,
  Pressable,
  StyleSheet,
  View,
  type AccessibilityActionEvent,
} from "react-native";
import { Gesture, GestureDetector } from "react-native-gesture-handler";
import { runOnJS } from "react-native-reanimated";

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

  const activeIndex = activeId
    ? drafts.findIndex((draft) => draft.exercise.id === activeId)
    : -1;
  const activeDraft = activeIndex >= 0 ? drafts[activeIndex] : undefined;
  const activeLayout = activeId ? layouts.get(activeId) : undefined;

  const startDrag = (exerciseId: string) => {
    const layout = layouts.get(exerciseId);
    const index = drafts.findIndex((draft) => draft.exercise.id === exerciseId);
    if (!layout || index < 0) return;

    dragStartTop.current = layout.y;
    dragTop.setValue(layout.y);
    setActiveId(exerciseId);
    setTargetIndex(index);
  };

  const moveDrag = (translationY: number, absoluteY: number) => {
    if (!activeId || !activeLayout) return;

    const nextTop = dragStartTop.current + translationY;
    dragTop.setValue(nextTop);
    const center = nextTop + activeLayout.height / 2;
    const nextTarget = findTargetIndex(drafts, layouts, center);
    setTargetIndex(nextTarget);
    onAutoScroll(absoluteY);
  };

  const finishDrag = () => {
    if (
      activeIndex >= 0 &&
      targetIndex !== undefined &&
      activeIndex !== targetIndex
    ) {
      onReorder(activeIndex, targetIndex);
    }

    setActiveId(undefined);
    setTargetIndex(undefined);
  };

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
          onLayout={(event) => {
            const { height, y } = event.nativeEvent.layout;
            setLayouts((current) => {
              const existing = current.get(draft.exercise.id);
              if (existing?.height === height && existing.y === y)
                return current;
              const next = new Map(current);
              next.set(draft.exercise.id, { height, y });
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
            onDragStart={() => startDrag(draft.exercise.id)}
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
  onDragEnd: () => void;
  onDragMove: (translationY: number, absoluteY: number) => void;
  onDragStart: () => void;
  onEdit: () => void;
  onMove: (toIndex: number) => void;
  unitSystem: UnitSystem;
}) {
  const dragGesture = useMemo(
    () =>
      Gesture.Pan()
        .activateAfterLongPress(180)
        .onStart(() => runOnJS(onDragStart)())
        .onUpdate((event) =>
          runOnJS(onDragMove)(event.translationY, event.absoluteY),
        )
        .onFinalize(() => runOnJS(onDragEnd)()),
    [onDragEnd, onDragMove, onDragStart],
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
        <GestureDetector gesture={dragGesture}>
          <View
            accessible
            accessibilityActions={accessibilityActions}
            accessibilityHint="Long press and drag to reorder"
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
          >
            <AppText style={styles.dragGlyph} tone="secondary">
              ≡
            </AppText>
          </View>
        </GestureDetector>

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
) {
  const target = drafts.findIndex((draft) => {
    const layout = layouts.get(draft.exercise.id);
    return layout ? center < layout.y + layout.height / 2 : false;
  });
  return target === -1 ? Math.max(0, drafts.length - 1) : target;
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
