const SATISFIED_FACTORS_PREFIX = "!";
const PERMISSIONS_PREFIX = "#";
const ITEMS_DELIM = ",";
const GROUP_DELIM = "&";
const REQUIRED_FACTORS_DELIM = "+";

const groupsRegex = buildGroupsRegex();

function buildGroupsRegex() {
  const groupPrefixes = SATISFIED_FACTORS_PREFIX + PERMISSIONS_PREFIX;
  const satisfiedFactorsGroup = `(?:${SATISFIED_FACTORS_PREFIX}([^${groupPrefixes}]*))?`;
  const permissionGroupsGroup = `(?:${PERMISSIONS_PREFIX}([^${groupPrefixes}]*))?`;
  return new RegExp(satisfiedFactorsGroup + permissionGroupsGroup);
}

function parseNumeric<T extends number>(value: string) {
  return parseInt(value, 32) as T;
}

export class FactorBasedPermissions<TFactor extends number, TPermission extends number> {
  private _satisfiedFactors = new Set<TFactor>();
  private _permissionsLookup = new Map<TPermission, TFactor[]>();
  private _permissionCheckLookup = new Map<TPermission, boolean | null>();
  private _serialized = "";

  public constructor(serialized?: string | null | undefined) {
    if (typeof serialized === "string") {
      const matches = groupsRegex.exec(serialized);
      this._parseSatisfiedFactors(matches?.[1] ?? null);
      this._parsePermissions(matches?.[2] ?? null);
      this._serialized = serialized;
    }
  }

  private _parseSatisfiedFactors(satisfiedFactorsGroup: string | null) {
    if (!satisfiedFactorsGroup)
      return;

    for (const factor of satisfiedFactorsGroup.split(ITEMS_DELIM)) {
      const factorNumeric = parseNumeric<TFactor>(factor);
      this._satisfiedFactors.add(factorNumeric);
    }
  }

  private _parsePermissions(permissionGroupsGroup: string | null) {
    if (!permissionGroupsGroup)
      return;

    for (const permissionGroup of permissionGroupsGroup.split(GROUP_DELIM)) {
      const [permissionsRaw, requiredFactorsRaw] = permissionGroup.split(REQUIRED_FACTORS_DELIM);
      const permissions = permissionsRaw?.split(ITEMS_DELIM)?.map(parseNumeric<TPermission>) ?? [];
      const requiredFactors = requiredFactorsRaw?.split(ITEMS_DELIM)?.map(parseNumeric<TFactor>) ?? [];

      for (const permission of permissions)
        this._permissionsLookup.set(permission, requiredFactors);
    }
  }

  public checkPermission(permission: TPermission) {
    const cachedResult = this._permissionCheckLookup.get(permission);

    if (cachedResult !== undefined)
      return cachedResult;

    const requiredFactors = this._permissionsLookup.get(permission);

    if (!requiredFactors) {
      this._permissionCheckLookup.set(permission, null);
      return null;
    }

    for (const requiredFactor of requiredFactors)
      if (!this._satisfiedFactors.has(requiredFactor)) {
        this._permissionCheckLookup.set(permission, false);
        return false;
      }

    this._permissionCheckLookup.set(permission, true);
    return true;
  }

  public getSatisfiedFactors(permission?: TPermission) {
    if (!permission)
      return [...this._satisfiedFactors];

    const cachedResult = this._permissionCheckLookup.get(permission);
    const requiredFactors = this._permissionsLookup.get(permission) ?? [];

    if (cachedResult === true)
      return requiredFactors;

    return requiredFactors.filter((factor) => this._satisfiedFactors.has(factor));
  }

  public getMissingFactors(permission: TPermission) {
    const cachedResult = this._permissionCheckLookup.get(permission);

    if (cachedResult === true)
      return [];

    const requiredFactors = this._permissionsLookup.get(permission) ?? [];
    return requiredFactors.filter((factor) => !this._satisfiedFactors.has(factor));
  }

  public get serialized() {
    return this._serialized;
  }
}
