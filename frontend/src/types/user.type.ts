import { z } from "zod";

/**
 * Base user scheme
 */
export const UserBaseSchema = z.object({
    username: z.string().min(1, { message: "Name is required" }),
    email: z.string().min(1, { message: "Email is required" }),
    emailConfirmed: z.boolean(),
    role: z.string().min(1, { message: "Role is required" }),
});

export const UserSchema = UserBaseSchema.extend({
  id: z.string().uuid(),
});

export const UsersArraySchema = z.array(UserSchema);

// Type definitions derived from the schemas
export type UserBase = z.infer<typeof UserBaseSchema>;
export type User = z.infer<typeof UserSchema>;
export type UserArray = z.infer<typeof UsersArraySchema>;

export type UserPageResponse = {
  success: boolean;
  message: string;
  pageIndex?: number;
  pageSize?: number;
  pageCount?: number;
  users?: UserArray;
}

export type SaveUserResponse = {
  success: boolean;
  message: string;
  user: User;
}

/**
 * Schema for updating an account DTO
 */
export const UpdateAccountDtoSchema = z.object({
  firstName: z.string().min(1, { message: "First name is required" }),
  lastName: z.string().min(1, { message: "Last name is required" }),
  email: z.string().email("Invalid e-mail address").or(z.literal("")).optional()
});

/**
 * DTO to send to backend to update an account
 */
export type UpdateAccountDto = z.infer<typeof UpdateAccountDtoSchema>;

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


