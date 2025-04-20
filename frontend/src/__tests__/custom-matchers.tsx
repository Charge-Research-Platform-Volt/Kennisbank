import { expect } from 'vitest'

// If you want to create a custom matcher:
//
// 1. Create a new function with return type: { pass: boolean, message: () => string }
//    The first argument will be passed via expect, the rest you have to pass in your function.
//    ex. toFoo(actual, arg1, arg2) -> expect(foo).toFoo(arg1, arg2)
//
// 2. Add it in the expect.extend object like below.
//
// 3. Add it to frontend/vitest.d.ts so that typescript will be happy.
//    Make sure to type everything correctly or you will get strange errors.

function toHaveFormDataFields(actual: FormData, fields: object) {
  for (const [key, value] of Object.entries(fields)) {
    if (actual.get(key) !== value) {
      return {
        pass: false,
        message: () => `at key: ${key}`,
        actual: actual.get(key),
        expected: value,
      }
    }
  }
  return { pass: true, message: () => "" }
}

expect.extend({
  toHaveFormDataFields,
})


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


