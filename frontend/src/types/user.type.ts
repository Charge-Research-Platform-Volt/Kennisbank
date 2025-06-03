import { z } from "zod";

/**
 * Base user scheme
 */
export const UserBaseSchema = z.object({
  email: z.string().min(1, { message: "Email is required" }),
  emailConfirmed: z.boolean(),
  role: z.string().min(1, { message: "Role is required" }),
});

export const UserSchema = UserBaseSchema.extend({
  id: z.string().uuid(),
});

// Schema to store simple user data
export const UserDataSchema = z.object({
  firstName: z.string().min(1, { message: "First name is required" }),
  lastName: z.string().min(1, { message: "Last name is required" }),
  email: z.string().email({ message: "Invalid email address" }),
  customAvatarVersion: z.number().int().nonnegative(),
});

export const UsersArraySchema = z.array(UserSchema);

// Type definitions derived from the schemas
export type UserBase = z.infer<typeof UserBaseSchema>;
export type User = z.infer<typeof UserSchema>;
export type UserData = z.infer<typeof UserDataSchema>;
export type UserArray = z.infer<typeof UsersArraySchema>;

export type UserPageResponse = {
  success: boolean;
  message: string;
  pageIndex?: number;
  pageSize?: number;
  pageCount?: number;
  users?: UserArray;
};

export type SaveUserResponse = {
  success: boolean;
  message: string;
  user: User;
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


