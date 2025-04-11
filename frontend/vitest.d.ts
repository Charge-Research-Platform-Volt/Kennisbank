import 'vitest'

interface CustomMatchers<R = unknown> {
  toHaveFormDataFields: (fields: Object) => R
}

declare module 'vitest' {
  interface Assertion<T = any> extends CustomMatchers<T> {}
  interface AsymmetricMatchersContaining extends CustomMatchers {}
}