import { redirect } from 'next/navigation';
import { FetchWithValidation } from './fetchWithValidation';
import { RoleResponse, RoleResponseSchema } from '@/types/authorization.type';

const API_URL: string = "http://backend:8080";
const UNAUTHORIZED_REDIRECT: string = "/unauthorized";
const UNAUTHENTICATED_REDIRECT: string = "/login";

/**
 * 
 * @returns Current user role, which is at the moment either an admin or a user
 */
export async function getCurrentUserRole(): Promise<RoleResponse>
{
    try
    {
        const result = await FetchWithValidation<RoleResponse>(RoleResponseSchema, `${API_URL}/roles/current`);

        if (!result.success)
            return { role: '', isAuthenticated: false };

        return result.data;
    }
    catch (error)
    {
        console.error('Failed to fetch user role:', error);
        return { role: '', isAuthenticated: false };
    }
}

/**
 * @summary Redirects unauthorized users
 */
export async function requireAuthentication(): Promise<void>
{
    const { isAuthenticated } = await getCurrentUserRole();

    if (!isAuthenticated)
        redirect(UNAUTHENTICATED_REDIRECT);
}

/**
 * 
 * @param requiredRole - The role required to go to that page
 * @param redirectPath - The path users will be redirected to instead
 * @summary Redirects users who do not have a sufficient role to the redirectPath
 */
export async function requireRoleWithRedirect(requiredRole: string, redirectPath: string = UNAUTHORIZED_REDIRECT): Promise<void>
{
    const { role, isAuthenticated } = await getCurrentUserRole();

    if (!isAuthenticated)
        redirect(UNAUTHENTICATED_REDIRECT);

    if (role !== requiredRole)
        redirect(redirectPath);
}

/**
 * 
 * @param allowedRoles - Roles allowed to visit the page
 * @param redirectPath - Path unauthorized users will be redirected to
 * @summary Users who do not have any of the allowed roles will be redirected to another page
 */
export async function requireAnyRoleWithRedirect(allowedRoles: string[], redirectPath: string = UNAUTHORIZED_REDIRECT): Promise<void>
{
    const { role, isAuthenticated} = await getCurrentUserRole();

    if (!isAuthenticated)
        redirect(UNAUTHENTICATED_REDIRECT);

    if (!allowedRoles.includes(role))
        redirect(redirectPath);
}

/**
 *
 * @param requiredRole - Role to check with
 * @returns Boolean indicating whether or not the current role of the user and the required role are equal
 */
export async function requireRole(requiredRole: string): Promise<boolean>
{
    const { role, isAuthenticated } = await getCurrentUserRole();

    if (!isAuthenticated)
        redirect(UNAUTHENTICATED_REDIRECT);

    return role === requiredRole;
}

/**
 * 
 * @param allowedRoles - The roles that are allowed to visit this page
 * @returns A boolean indicating whether or not the user is authorized to visit the page
 */
export async function requireAnyRole(allowedRoles: string[]): Promise<boolean>
{
    const { role, isAuthenticated } = await getCurrentUserRole();

    if (!isAuthenticated)
        redirect(UNAUTHENTICATED_REDIRECT);

    return allowedRoles.includes(role);
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


