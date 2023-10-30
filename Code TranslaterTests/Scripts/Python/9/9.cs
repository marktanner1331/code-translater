class Solution
{
	List<Object> ans = new List<object>();
	
	List<int> getLonelyNodes(TreeNode? root)
	{
		void helper(object root)
		{
			if (root.left is null && root.right)
			{
				this.ans.append(root.right.val);
				return helper(root.right);
			}
			if (root.left && root.right is null)
			{
				this.ans.append(root.left.val);
				return helper(root.left);
			}
			if (root.left && root.right)
			{
				return new List<object> { helper(root.left), helper(root.right) };
			}
			if (root.left is null && root.right is null)
			{
				return;
			}
		}
		
		helper(root);
		ans = this.ans;
		this.ans = new List<object>();
		return ans;
	}
}