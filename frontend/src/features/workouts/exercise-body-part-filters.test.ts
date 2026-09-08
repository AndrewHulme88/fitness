import { getBodyPartSearchFilters } from "./exercise-body-part-filters";

describe("exercise body-part filters", () => {
  it("searches every primary leg muscle", () => {
    expect(getBodyPartSearchFilters("legs")).toEqual([
      { primaryMuscle: "quadriceps" },
      { primaryMuscle: "hamstrings" },
      { primaryMuscle: "glutes" },
      { primaryMuscle: "calves" },
    ]);
  });

  it("uses the catalogue cardio category", () => {
    expect(getBodyPartSearchFilters("cardio")).toEqual([
      { category: "cardio" },
    ]);
  });
});
